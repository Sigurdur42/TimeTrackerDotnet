using System;
using System.Collections.Generic;
using System.Linq;
using TimeTracker.Models;

namespace TimeTracker.Controller;

public class TimeRecordCalculator
{
    public const double DefaultDailyTargetHours = WorkingTimeRules.DefaultDailyTargetHours;

    private readonly WorkingTimeRules[] _rules;

    /// <param name="dailyTargetHours">Target working hours of a weekday, used by the rules since <see cref="WorkingTimeRules.Rules2026Start" /></param>
    public TimeRecordCalculator(double dailyTargetHours = DefaultDailyTargetHours) : this(WorkingTimeRules.CreateDefault(dailyTargetHours))
    {
    }

    /// <param name="rules">The rule sets, the first one must be valid from the earliest recorded day</param>
    public TimeRecordCalculator(IEnumerable<WorkingTimeRules> rules)
    {
        _rules = rules.OrderBy(_ => _.ValidFrom).ToArray();
        if (_rules.Length == 0) throw new ArgumentException("At least one rule set is needed", nameof(rules));
    }

    /// <summary>
    ///     The rule set valid on the given day
    /// </summary>
    public WorkingTimeRules GetRules(DateOnly date)
    {
        var result = _rules[0];
        foreach (var rules in _rules)
            if (rules.ValidFrom <= date)
                result = rules;

        return result;
    }

    /// <summary>
    ///     Target working minutes of a weekday on the given day
    /// </summary>
    public decimal GetDailyTargetMinutes(DateOnly date)
    {
        return GetRules(date).DailyTargetMinutes;
    }

    public TimeRecordCategorized[] CalculateCategorySummary(TimeRecord[] timeRecords)
    {
        var result = new List<TimeRecordCategorized>();

        foreach (var dateGroup in timeRecords.GroupBy(_ => _.Date))
        foreach (var item in dateGroup.GroupBy(_ => _.Category, StringComparer.OrdinalIgnoreCase))
        {
            double work = 0, travelActive = 0, travelPassive = 0;
            foreach (var record in item)
            {
                var minutes = record.Duration.TotalMinutes;
                if (record.TravelActive) travelActive += minutes;
                else if (record.TravelPassive) travelPassive += minutes;
                else work += minutes;
            }

            result.Add(new TimeRecordCategorized
            {
                Date = dateGroup.Key,
                Category = item.Key,
                WorkMinutes = (decimal)work,
                TravelActiveMinutes = (decimal)travelActive,
                TravelPassiveMinutes = (decimal)travelPassive
            });
        }

        return result.ToArray();
    }

    public TimeRecordByDay[] CalculateByDay(TimeRecord[] timeRecords)
    {
        var result = new List<TimeRecordByDay>();
        foreach (var dateGroup in timeRecords.GroupBy(_ => _.Date))
        {
            double workSum = 0, travelActiveSum = 0, travelPassiveSum = 0, forceOvertimeSum = 0, flextimeSum = 0, sickSum = 0;
            TimeOnly? firstStart = null, firstWorkStart = null;

            foreach (var record in dateGroup)
            {
                var minutes = record.Duration.TotalMinutes;
                if (record.IsFlextime)
                {
                    flextimeSum += minutes;
                    continue;
                }

                if (record.IsSick)
                {
                    sickSum += minutes;
                    continue;
                }

                if (record.TravelActive)
                {
                    travelActiveSum += minutes;
                }
                else if (record.TravelPassive)
                {
                    travelPassiveSum += minutes;
                }
                else
                {
                    workSum += minutes;
                    if (firstWorkStart is null || record.Start < firstWorkStart) firstWorkStart = record.Start;
                }

                if (record.AllOvertime) forceOvertimeSum += minutes;
                if (firstStart is null || record.Start < firstStart) firstStart = record.Start;
            }

            var day = new TimeRecordByDay
            {
                Date = dateGroup.Key,
                TravelActiveMinutes = (decimal)travelActiveSum,
                TravelPassiveMinutes = (decimal)travelPassiveSum,
                WorkMinutes = (decimal)workSum,
                FlextimeMinutes = (decimal)flextimeSum,
                SickMinutes = (decimal)sickSum,
                Start = firstWorkStart ?? firstStart ?? TimeOnly.MinValue
            };

            var forceOvertime = (decimal)forceOvertimeSum;

            // Flextime only counts on days without work or travel. Otherwise the missing time is already
            // part of the working time compared to the target hours. A day sick for the rest is compared to the target.
            if (firstStart is null && (flextimeSum > 0 || sickSum <= 0))
                day.OvertimeMinutes = -day.FlextimeMinutes;
            else
                ApplyRules(day, forceOvertime, GetRules(day.Date));

            result.Add(day);
        }

        return result.ToArray();
    }

    /// <summary>
    ///     Credits the recorded time of the day according to the rules: limits work and active travel, adds the weekend
    ///     surcharge, limits passive travel and calculates the overtime.
    /// </summary>
    private static void ApplyRules(TimeRecordByDay day, decimal forceOvertime, WorkingTimeRules rules)
    {
        var work = day.WorkMinutes + day.TravelActiveMinutes;
        var creditedWork = rules.MaxWorkMinutes is { } maxWork ? Math.Min(work, maxWork) : work;
        day.MaxWorkMinutes = rules.MaxWorkMinutes;
        day.WorkCapLossMinutes = work - creditedWork;

        var surchargeFactor = day.Date.DayOfWeek switch
        {
            DayOfWeek.Saturday => rules.SaturdaySurcharge,
            DayOfWeek.Sunday => rules.SundaySurcharge,
            _ => 0m
        };
        day.SurchargeMinutes = creditedWork * surchargeFactor;

        var passive = day.TravelPassiveMinutes;
        decimal creditedPassive;
        if (work <= 0)
            creditedPassive = rules.LimitPassiveTravelWithoutWorkToTarget ? Math.Min(passive, rules.DailyTargetMinutes) : passive;
        else if (rules.FullPassiveTravelWithWorkMinutes is { } fullPassive)
            creditedPassive = Math.Min(passive, fullPassive) + Math.Max(passive - fullPassive, 0) * rules.ReducedPassiveTravelFactor;
        else
            creditedPassive = passive;
        day.PassiveTravelLossMinutes = passive - creditedPassive;

        // Sickness is credited on weekdays only, as there is no target on weekends. It is not work, so it is
        // neither limited nor surcharged.
        var creditedSick = day.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0 : day.SickMinutes;

        var credited = creditedWork + day.SurchargeMinutes + creditedPassive + creditedSick;
        day.OvertimeMinutes = CalculateOvertime(day.Date, credited, Math.Min(forceOvertime, credited), rules.DailyTargetMinutes);
    }

    /// <summary>
    ///     On weekends all time is overtime, on weekdays the time beyond the target. Time marked as all overtime is not
    ///     compared to the target.
    /// </summary>
    private static decimal CalculateOvertime(DateOnly date, decimal total, decimal forceOvertime, decimal targetMinutes)
    {
        var totalNormal = total - forceOvertime;
        return date.DayOfWeek switch
        {
            DayOfWeek.Sunday or DayOfWeek.Saturday => total,
            _ => forceOvertime > 0 ? forceOvertime + (totalNormal > 0 ? totalNormal - targetMinutes : 0) : totalNormal - targetMinutes
        };
    }

    public TimeRecordByMonth[] CalculateByMonth(TimeRecordByDay[] timeRecords)
    {
        return timeRecords
            .GroupBy(_ => (_.Date.Year, _.Date.Month))
            .Select(dateGroup => new TimeRecordByMonth
            {
                Date = dateGroup.First().Date,
                OvertimeMinutes = dateGroup.Sum(_ => _.OvertimeMinutes)
            })
            .ToArray();
    }

    /// <summary>
    ///     Sets the overtime accumulated within each week (Monday to Sunday) and marks the last recorded day of each week.
    ///     <paramref name="days" /> must be sorted ascending by date.
    /// </summary>
    public void CalculateWeeklyTotals(TimeRecordByDay[] days)
    {
        var total = TimeSpan.Zero;
        TimeRecordByDay? lastDay = null;
        foreach (var day in days)
        {
            // Compare the weeks instead of looking for a Monday, as the Monday may have no record (e.g. holiday)
            if (lastDay != null && GetStartOfWeek(day.Date) != GetStartOfWeek(lastDay.Date))
            {
                lastDay.IsLastOfWeek = true;
                total = TimeSpan.Zero;
            }

            total += day.Overtime;
            day.OvertimeThisWeek = total;

            lastDay = day;
        }

        if (lastDay != null) lastDay.IsLastOfWeek = true;
    }

    /// <summary>
    ///     Sets the overtime accumulated since the first month. <paramref name="months" /> must be sorted ascending by date.
    /// </summary>
    public void CalculateRunningTotals(TimeRecordByMonth[] months)
    {
        var total = TimeSpan.Zero;
        foreach (var month in months)
        {
            total += month.Overtime;
            month.OvertimeTotal = total;
        }
    }

    private static DateOnly GetStartOfWeek(DateOnly date)
    {
        return date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
    }

    public decimal CalculateTotalOvertime(IEnumerable<TimeRecordByMonth> records)
    {
        return records.Sum(m => m.OvertimeMinutes);
    }

    public TimeRecordExcel[] CalculateExcel(TimeRecordByDay[] timeRecords)
    {
        // Days with flextime or sickness only have no times to be entered
        return timeRecords
            .Where(_ => _.WorkMinutes > 0 || _.TravelActiveMinutes > 0 || _.TravelPassiveMinutes > 0 || (_.FlextimeMinutes == 0 && _.SickMinutes == 0))
            .Select(_ =>
            {
                var result = new TimeRecordExcel { Date = _.Date };

                // The span covered so far: the work time, then each travel time is wrapped around it
                TimeOnly? spanStart = null, spanEnd = null;

                var isTravelOnly = _.WorkMinutes <= 0 && (_.TravelActiveMinutes > 0 || _.TravelPassiveMinutes > 0);
                if (!isTravelOnly)
                {
                    spanStart = result.Start = _.Start;
                    spanEnd = result.End = _.Start.AddMinutes((double)_.WorkMinutes + 45);
                }

                (result.TravelActiveStart, result.TravelActiveEnd) = WrapTravel(_.TravelActiveMinutes);
                (result.TravelPassiveStart, result.TravelPassiveEnd) = WrapTravel(_.TravelPassiveMinutes);

                return result;

                // Without a span yet the travel starts with the day, otherwise half of it is put before and after the span
                (TimeOnly?, TimeOnly?) WrapTravel(decimal minutes)
                {
                    if (minutes <= 0) return (null, null);

                    if (spanStart is null || spanEnd is null)
                    {
                        spanStart = _.Start;
                        spanEnd = _.Start.AddMinutes((double)minutes);
                    }
                    else
                    {
                        var half = (double)minutes / 2;
                        spanStart = spanStart.Value.AddMinutes(-half);
                        spanEnd = spanEnd.Value.AddMinutes(half);
                    }

                    return (spanStart, spanEnd);
                }
            })
            .ToArray();
    }
}
