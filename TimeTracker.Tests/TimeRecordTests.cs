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
    [TestCase("  duna : travel", "Duna")]
    [TestCase("gmk: a", "gmk: a")]
    [TestCase("GMK", "GMK")]
    [TestCase(":foo", ":foo")]
    [TestCase("a", "A")]
    [TestCase("ärger: x", "Ärger")]
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
        Assert.That(result, Is.EqualTo(expectedMonth));
    }

    [TestCase("09:00", "12:00", "3:00")]
    [TestCase("06:00", "12:50", "6:50")]
    [TestCase("06:00", "16:30", "10:30")]
    [TestCase("14:30", "15:15", "00:45")]
    [TestCase("06:10", "12:30", "06:20")]
    [TestCase("06:10", "11:30", "05:20")]
    [TestCase("08:00", "08:00", "00:00")]
    [TestCase("22:00", "02:00", "04:00")]
    public void DurationTests(string start, string end, string expectedDuration)
    {
        var target = new TimeRecord
        {
            Start = TimeOnly.Parse(start),
            End = TimeOnly.Parse(end)
        };
        var duration = target.Duration;
        Assert.That(duration, Is.EqualTo(TimeSpan.Parse(expectedDuration)));
    }

    [TestCase(nameof(TimeRecord.Start), nameof(TimeRecord.Duration))]
    [TestCase(nameof(TimeRecord.End), nameof(TimeRecord.Duration))]
    [TestCase(nameof(TimeRecord.Comment), nameof(TimeRecord.Category))]
    [TestCase(nameof(TimeRecord.Date), nameof(TimeRecord.Month))]
    public void ChangeRaisesDerivedProperty(string property, string derivedProperty)
    {
        var target = new TimeRecord();
        var changed = new List<string?>();
        target.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        new TimeRecord
        {
            Date = new DateOnly(2024, 2, 5),
            Start = new TimeOnly(8, 0),
            End = new TimeOnly(9, 0),
            Comment = "misc"
        }.CopyTo(target);

        Assert.That(changed, Does.Contain(property).And.Contain(derivedProperty));
    }

    [Test]
    public void SettingSameValuesRaisesNothing()
    {
        var target = new TimeRecord { Date = new DateOnly(2024, 2, 5), Start = new TimeOnly(8, 0), End = new TimeOnly(9, 0), Comment = "misc" };
        var raised = false;
        target.PropertyChanged += (_, _) => raised = true;

        new TimeRecord { Date = new DateOnly(2024, 2, 5), Start = new TimeOnly(8, 0), End = new TimeOnly(9, 0), Comment = "misc" }.CopyTo(target);

        Assert.That(raised, Is.False);
    }
}
