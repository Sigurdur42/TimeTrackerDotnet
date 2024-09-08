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
            var byCategory = dateGroup.GroupBy(_ => _.Category);
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
            var first = dateGroup.First();

            var work = (decimal)dateGroup.Where(_ => !_.Travel).Sum(_ => _.Duration.TotalMinutes);
            var travel = (decimal)dateGroup.Where(_ => _.Travel).Sum(_ => _.Duration.TotalMinutes);

            var forceOvertime = (decimal)dateGroup.Where(_ => _.AllOvertime).Sum(_ => _.Duration.TotalMinutes);
            var totalNormal = work + travel - forceOvertime;

            var total = first.Date.DayOfWeek switch
            {
                DayOfWeek.Sunday or DayOfWeek.Saturday => totalNormal + forceOvertime,
                _ => totalNormal - _eightHours + forceOvertime
            };

            var record = new TimeRecordByDay
            {
                Date = first.Date,
                OvertimeMinutes = total,
            };

            result.Add(record);
        }

        return result.ToArray();
    }

    public TimeRecordByMonth[] CalculateByMonth(TimeRecord[] timeRecords)
    {
        var result = new List<TimeRecordByMonth>();
        var byDate = timeRecords.GroupBy(_ => _.Month);
        foreach (var dateGroup in byDate)
        {
            var first = dateGroup.First();
            var overTimeTotal = 0m;
            var total = 0m;

            foreach (var dayItem in dateGroup.GroupBy(_ => _.Date))
            {
                var work = (decimal)dayItem.Where(_ => !_.Travel).Sum(_ => _.Duration.TotalMinutes);
                var travel = (decimal)dayItem.Where(_ => _.Travel).Sum(_ => _.Duration.TotalMinutes);

                var forceOvertime = (decimal)dayItem.Where(_ => _.AllOvertime).Sum(_ => _.Duration.TotalMinutes);
                var totalNormal = work + travel - forceOvertime;

                var overtimeForDay = first.Date.DayOfWeek switch
                {
                    DayOfWeek.Sunday or DayOfWeek.Saturday => totalNormal + forceOvertime,
                    _ => totalNormal - _eightHours + forceOvertime
                };
                total += totalNormal + forceOvertime;
                overTimeTotal += overtimeForDay;
            }

            var record = new TimeRecordByMonth
            {
                Date = first.Date,
                OvertimeMinutes = overTimeTotal,
                TotalMinutes = total,
            };

            result.Add(record);
        }

        return result.ToArray();
    }

    public decimal CalculateTotalOvertime(IEnumerable<TimeRecordByMonth> records)
    {
        return records.Sum(_ => _.OvertimeMinutes);
    }
}