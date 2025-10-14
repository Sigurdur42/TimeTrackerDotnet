using System.Globalization;
using TimeTracker.Models;

namespace TimeTracker.Tests;

[SetCulture("de-de")]
public class TimeRecordTests
{
    [TestCase("", "")]
    [TestCase(null, "")]
    [TestCase("  ", "")]
    [TestCase("misc", "Misc")]
    [TestCase("misc: some tasks", "Misc")]
    [TestCase("DUNA: Travel", "DUNA")]
    [TestCase("DUNA", "DUNA")]
    [TestCase("DUNA ", "DUNA")]
    public void CategoryTests(string? comment, string expectedCategory)
    {
        var target = new TimeRecord
        {
            Comment = comment
        };

        var result = target.Category;
        Assert.That(result, Is.EqualTo(expectedCategory));
    }

    [TestCase("01.01.2024", "01.2024")]
    [TestCase("31.01.2024", "01.2024")]
    public void MonthTests(string date, string expectedMonth)
    {
        var refDate = DateOnly.Parse(date, new CultureInfo("de-de"));
        var target = new TimeRecord
        {
            Date = refDate
        };

        var result = target.Month;
        Assert.That(expectedMonth, Is.EqualTo(result));
    }

    [TestCase("09:00", "12:00", "3:00")]
    [TestCase("06:00", "12:50", "6:50")]
    [TestCase("06:00", "16:30", "10:30")]
    [TestCase("14:30", "15:15", "00:45")]
    [TestCase("06:10", "12:30", "06:20")]
    [TestCase("06:10", "11:30", "05:20")]
    public void DurationTests(string start, string end, string expectedDuration)
    {
        var target = new TimeRecord
        {
            Start = TimeOnly.Parse(start),
            End = TimeOnly.Parse(end)
        };
        var duration = target.Duration;
        Assert.That(TimeSpan.Parse(expectedDuration), Is.EqualTo(duration));
    }
}