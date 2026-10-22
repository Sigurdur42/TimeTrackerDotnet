using System;
using System.Collections.Generic;
using System.Linq;
using TimeTracker.Models;

namespace TimeTracker.Controller;

public class TimeRecordCalculator
{
    private const decimal EightHours = 8 * 60;

    public TimeRecordCategorized[] CalculateCategorySummary(TimeRecord[] timeRecords)
    {
        var result = new List<TimeRecordCategorized>();

        foreach (var dateGroup in timeRecords.GroupBy(_ => _.Date))
        foreach (var item in dateGroup.GroupBy(_ => _.Category, StringComparer.OrdinalIgnoreCase))
        {
            double work = 0, travel = 0;
            foreach (var record in item)
            {
                if (record.Travel) travel += record.Duration.TotalMinutes;
                else work += record.Duration.TotalMinutes;
            }

            result.Add(new TimeRecordCategorized
            {
                Date = dateGroup.Key,
                Category = item.Key,
                WorkMinutes = (decimal)work,
                TravelMinutes = (decimal)travel
            });
        }

        return result.ToArray();
    }

    public TimeRecordByDay[] CalculateByDay(TimeRecord[] timeRecords)
    {
        var result = new List<TimeRecordByDay>();
        foreach (var dateGroup in timeRecords.GroupBy(_ => _.Date))
        {
            double workSum = 0, travelSum = 0, forceOvertimeSum = 0, flextimeSum = 0;
            TimeOnly? firstStart = null, firstWorkStart = null;

            foreach (var record in dateGroup)
            {
                var minutes = record.Duration.TotalMinutes;
                if (record.IsFlextime)
                {
                    flextimeSum += minutes;
                    continue;
                }

                if (record.Travel)
                {
                    travelSum += minutes;
                }
                else
                {
                    workSum += minutes;
                    if (firstWorkStart is null || record.Start < firstWorkStart) firstWorkStart = record.Start;
                }

                if (record.AllOvertime) forceOvertimeSum += minutes;
                if (firstStart is null || record.Start < firstStart) firstStart = record.Start;
            }

            var work = (decimal)workSum;
            var travel = (decimal)travelSum;
            var forceOvertime = (decimal)forceOvertimeSum;
            var flextime = (decimal)flextimeSum;
            var totalNormal = work + travel - forceOvertime;

            // Flextime only counts on days without any other record. Otherwise the missing time is already
            // part of the working time compared to the eight hours.
            var total = firstStart is null
                ? -flextime
                : dateGroup.Key.DayOfWeek switch
                {
                    DayOfWeek.Sunday or DayOfWeek.Saturday => totalNormal + forceOvertime,
                    _ => forceOvertime > 0 ? forceOvertime + (totalNormal > 0 ? totalNormal - EightHours : 0) : totalNormal - EightHours + forceOvertime
                };

            result.Add(new TimeRecordByDay
            {
                Date = dateGroup.Key,
                OvertimeMinutes = total,
                TravelMinutes = travel,
                WorkMinutes = work,
                FlextimeMinutes = flextime,
                Start = firstWorkStart ?? firstStart ?? TimeOnly.MinValue
            });
        }

        return result.ToArray();
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
        return timeRecords.Where(_ => _.WorkMinutes > 0 || _.TravelMinutes > 0 || _.FlextimeMinutes == 0).Select(_ =>
            {
                var hasWorkTime = _.WorkMinutes > 0;
                var hasTravelTime = _.TravelMinutes > 0;

                if (!hasWorkTime && hasTravelTime)
                    return new TimeRecordExcel
                    {
                        Date = _.Date,
                        TravelStart = _.Start,
                        TravelEnd = _.Start.AddMinutes((double)_.TravelMinutes)
                    };

                var startTime = _.Start;
                var endTime = _.Start.AddMinutes((double)_.WorkMinutes + 45);
                var halfTravelTime = (double)_.TravelMinutes / 2;

                return new TimeRecordExcel
                {
                    Date = _.Date,
                    Start = startTime,
                    End = endTime,
                    TravelStart = hasTravelTime ? startTime.AddMinutes(-halfTravelTime) : null,
                    TravelEnd = hasTravelTime ? endTime.AddMinutes(halfTravelTime) : null
                };
            })
            .ToArray();
    }
}
