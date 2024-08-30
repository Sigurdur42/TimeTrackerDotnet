using System.Collections.Generic;
using System.Linq;
using TimeTracker.Models;

namespace TimeTracker.Controller;

public class TimeRecordCalculator
{
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
}