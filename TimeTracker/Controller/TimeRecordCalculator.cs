using System;
using System.Collections.Generic;
using System.Linq;
using TimeTracker.Models;

namespace TimeTracker.Controller;

public class TimeRecordCalculator
{
    private const decimal EightHours = 8 * 60;

    /// <summary>
    ///     First day the rules of <see cref="ApplyRulesOf2026" /> apply, earlier days are calculated as before
    /// </summary>
    public static readonly DateOnly RulesOf2026Start = new(2026, 12, 1);

    public const decimal MaxWorkMinutes = 10 * 60;
    public const decimal FullPassiveTravelMinutes = 6 * 60;
    public const decimal ReducedPassiveTravelFactor = 0.5m;
    public const double DefaultDailyTargetHours = 8;

    private readonly decimal _dailyTargetMinutes;

    /// <param name="dailyTargetHours">Target working hours of a weekday, used by the rules since <see cref="RulesOf2026Start" /></param>
    public TimeRecordCalculator(double dailyTargetHours = DefaultDailyTargetHours)
    {
        if (dailyTargetHours is <= 0 or > 24) throw new ArgumentOutOfRangeException(nameof(dailyTargetHours), dailyTargetHours, "Must be between 0 and 24 hours");
        _dailyTargetMinutes = (decimal)dailyTargetHours * 60;
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
            double workSum = 0, travelActiveSum = 0, travelPassiveSum = 0, forceOvertimeSum = 0, flextimeSum = 0;
            TimeOnly? firstStart = null, firstWorkStart = null;

            foreach (var record in dateGroup)
            {
                var minutes = record.Duration.TotalMinutes;
                if (record.IsFlextime)
                {
                    flextimeSum += minutes;
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
                Start = firstWorkStart ?? firstStart ?? TimeOnly.MinValue
            };

            var forceOvertime = (decimal)forceOvertimeSum;

            // Flextime only counts on days without any other record. Otherwise the missing time is already
            // part of the working time compared to the target hours.
            if (firstStart is null)
                day.OvertimeMinutes = -day.FlextimeMinutes;
            else if (day.Date < RulesOf2026Start)
                day.OvertimeMinutes = CalculateOvertime(day.Date, day.WorkMinutes + day.TravelActiveMinutes + day.TravelPassiveMinutes, forceOvertime, EightHours);
            else
                ApplyRulesOf2026(day, forceOvertime);

            result.Add(day);
        }

        return result.ToArray();
    }

    /// <summary>
    ///     Rules valid since <see cref="RulesOf2026Start" />:
    ///     <list type="bullet">
    ///         <item>Work and active travel count up to <see cref="MaxWorkMinutes" /> a day, the rest is lost</item>
    ///         <item>Work and active travel get a surcharge of 25% on Saturday and 100% on Sunday</item>
    ///         <item>Passive travel on days without work counts up to the daily target hours</item>
    ///         <item>Passive travel on days with work counts fully up to <see cref="FullPassiveTravelMinutes" />, the rest by half</item>
    ///     </list>
    /// </summary>
    private void ApplyRulesOf2026(TimeRecordByDay day, decimal forceOvertime)
    {
        var work = day.WorkMinutes + day.TravelActiveMinutes;
        var creditedWork = Math.Min(work, MaxWorkMinutes);
        day.WorkCapLossMinutes = work - creditedWork;

        var surchargeFactor = day.Date.DayOfWeek switch
        {
            DayOfWeek.Saturday => 0.25m,
            DayOfWeek.Sunday => 1m,
            _ => 0m
        };
        day.SurchargeMinutes = creditedWork * surchargeFactor;

        var passive = day.TravelPassiveMinutes;
        var creditedPassive = work > 0
            ? Math.Min(passive, FullPassiveTravelMinutes) + Math.Max(passive - FullPassiveTravelMinutes, 0) * ReducedPassiveTravelFactor
            : Math.Min(passive, _dailyTargetMinutes);
        day.PassiveTravelLossMinutes = passive - creditedPassive;

        var credited = creditedWork + day.SurchargeMinutes + creditedPassive;
        day.OvertimeMinutes = CalculateOvertime(day.Date, credited, Math.Min(forceOvertime, credited), _dailyTargetMinutes);
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
        // Days with flextime only have no times to be entered
        return timeRecords
            .Where(_ => _.WorkMinutes > 0 || _.TravelActiveMinutes > 0 || _.TravelPassiveMinutes > 0 || _.FlextimeMinutes == 0)
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
