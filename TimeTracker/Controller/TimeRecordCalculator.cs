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
            double workSum = 0, travelSum = 0, forceOvertimeSum = 0;
            TimeOnly? firstStart = null, firstWorkStart = null;

            foreach (var record in dateGroup)
            {
                var minutes = record.Duration.TotalMinutes;
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
            var totalNormal = work + travel - forceOvertime;

            var total = dateGroup.Key.DayOfWeek switch
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
                Start = firstWorkStart ?? firstStart!.Value
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

    public decimal CalculateTotalOvertime(IEnumerable<TimeRecordByMonth> records)
    {
        return records.Sum(m => m.OvertimeMinutes);
    }

    public TimeRecordExcel[] CalculateExcel(TimeRecordByDay[] timeRecords)
    {
        return timeRecords.Select(_ =>
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
