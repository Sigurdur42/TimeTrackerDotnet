using System.Globalization;
using TimeTracker.Models;

namespace TimeTracker.Tests;

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
    public void CategoryTests(string comment, string expectedCategory)
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
    public void MonthTests(string date, string expetedMonth)
    {
        var refDate = DateOnly.Parse(date, new CultureInfo("de-de"));
        var target = new TimeRecord
        {
            Date = refDate
        };

        var result = target.Month;
        Assert.That(result, Is.EqualTo(expetedMonth));
    }
}