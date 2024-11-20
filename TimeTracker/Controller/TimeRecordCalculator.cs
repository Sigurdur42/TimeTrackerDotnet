using System;
using System.Collections.Generic;
using System.Linq;
using TimeTracker.Models;

namespace TimeTracker.Controller;

public class TimeRecordCalculator
{
    readonly decimal _eightHours = 8 * 60;

    public TimeRecordCategorized[] CalculateCategorySummary(TimeRecord[] timeRecords)
    {
        var result = new List<TimeRecordCategorized>();

        var byDate = timeRecords.GroupBy(_ => _.Date);
        foreach (var dateGroup in byDate)
        {
            var byCategory = dateGroup.GroupBy(_ => _.Category.ToLowerInvariant());
            foreach (var item in byCategory)
            {
                var first = item.First();
                var categoryItem = new TimeRecordCategorized
                {
                    Date = first.Date,
                    Category = first.Category,
                    WorkMinutes = (decimal)item.Where(_ => !_.Travel).Sum(_ => _.Duration.TotalMinutes),
                    TravelMinutes = (decimal)item.Where(_ => _.Travel).Sum(_ => _.Duration.TotalMinutes),
                };
                result.Add(categoryItem);
            }
        }

        return result.ToArray();
    }

    public TimeRecordByDay[] CalculateByDay(TimeRecord[] timeRecords)
    {
        var result = new List<TimeRecordByDay>();
        var byDate = timeRecords.GroupBy(_ => _.Date);
        foreach (var dateGroup in byDate)
        {
            var first = dateGroup.OrderBy(_ => _.Start).First();

            var work = (decimal)dateGroup.Where(_ => !_.Travel).Sum(_ => _.Duration.TotalMinutes);
            var travel = (decimal)dateGroup.Where(_ => _.Travel).Sum(_ => _.Duration.TotalMinutes);

            var forceOvertime = (decimal)dateGroup.Where(_ => _.AllOvertime).Sum(_ => _.Duration.TotalMinutes);
            var totalNormal = work + travel - forceOvertime;

            var total = first.Date.DayOfWeek switch
            {
                DayOfWeek.Sunday or DayOfWeek.Saturday => totalNormal + forceOvertime,
                _ => forceOvertime > 0 ? forceOvertime + (totalNormal > 0 ? totalNormal - _eightHours : 0) : totalNormal - _eightHours + forceOvertime
            };

            var startTime = dateGroup.OrderBy(_ => _.Start).FirstOrDefault(_=>!_.Travel)?.Start ?? first.Start;

            var record = new TimeRecordByDay
            {
                Date = first.Date,
                OvertimeMinutes = total,
                TravelMinutes = travel,
                WorkMinutes = work,
                Start = startTime,
            };

            result.Add(record);
        }

        return result.ToArray();
    }

    public TimeRecordByMonth[] CalculateByMonth(TimeRecordByDay[] timeRecords)
    {
        var result = new List<TimeRecordByMonth>();
        var byMonth = timeRecords.GroupBy(_ => _.Month);
        foreach (var dateGroup in byMonth)
        {
            var first = dateGroup.First();
            var overTimeTotal = (decimal)dateGroup.Sum(_ => _.OvertimeMinutes);
            var record = new TimeRecordByMonth
            {
                Date = first.Date,
                OvertimeMinutes = overTimeTotal,
            };

            result.Add(record);
        }

        return result.ToArray();
    }

    public decimal CalculateTotalOvertime(IEnumerable<TimeRecordByMonth> records)
    {
        return records.Sum(m => m.OvertimeMinutes);
    }

    public TimeRecordExcel[] CalculateExcel(TimeRecordByDay[] timeRecords)
    {
        return timeRecords.Select((_) =>
            {
                var hasWorkTime = _.WorkMinutes > 0;

                TimeOnly? startTime = _.Start;
                TimeOnly? endTime = _.Start.AddMinutes((double)_.WorkMinutes + 45);
                var hasTravelTime = _.TravelMinutes > 0;
                var halfTravelTime = hasTravelTime ? (double)_.TravelMinutes / 2 : 0d;

                TimeOnly? travelStart = null;
                TimeOnly? travelEnd = null;

                if (!hasWorkTime && hasTravelTime)
                {
                    travelStart = startTime;
                    travelEnd = startTime?.AddMinutes((double)_.TravelMinutes);

                    startTime = null;
                    endTime = null;
                }
                else
                {
                    travelStart = hasTravelTime ? startTime?.AddMinutes(-1 * halfTravelTime) : null;
                    travelEnd = hasTravelTime ? endTime?.AddMinutes(halfTravelTime) : null;
                }

                return new TimeRecordExcel()
                {
                    Date = _.Date,
                    Start = startTime,
                    End = endTime,
                    TravelStart = travelStart,
                    TravelEnd = travelEnd,
                };
            }).ToArray();

    }
}