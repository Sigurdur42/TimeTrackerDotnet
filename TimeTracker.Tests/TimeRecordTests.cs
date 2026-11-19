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

    [Test]
    public void ActiveAndPassiveTravelExcludeEachOther()
    {
        var target = new TimeRecord { TravelActive = true };

        target.TravelPassive = true;
        Assert.Multiple(() =>
        {
            Assert.That(target.TravelPassive, Is.True);
            Assert.That(target.TravelActive, Is.False);
        });

        target.TravelActive = true;
        Assert.Multiple(() =>
        {
            Assert.That(target.TravelActive, Is.True);
            Assert.That(target.TravelPassive, Is.False);
        });
    }

    [Test]
    public void CopyToCopiesPassiveTravel()
    {
        var source = new TimeRecord { TravelPassive = true };
        var target = new TimeRecord { TravelActive = true };

        source.CopyTo(target);

        Assert.Multiple(() =>
        {
            Assert.That(target.TravelPassive, Is.True);
            Assert.That(target.TravelActive, Is.False);
        });
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

    private static TimeRecord Sample()
    {
        return new TimeRecord { Date = new DateOnly(2024, 2, 5), Start = new TimeOnly(8, 0), End = new TimeOnly(9, 0), Comment = "misc", TravelPassive = true };
    }

    [Test]
    public void RecordsWithEqualValuesAreEqual()
    {
        var target = Sample();
        object other = Sample();
        object? nothing = null;
        object otherType = "misc";

        var equalsOther = target.Equals(other);
        var equalsItself = target.Equals((object)target);
        var equalsNull = target.Equals(nothing);
        var equalsOtherType = target.Equals(otherType);

        Assert.Multiple(() =>
        {
            Assert.That(equalsOther, Is.True);
            Assert.That(equalsItself, Is.True);
            Assert.That(target.GetHashCode(), Is.EqualTo(other.GetHashCode()));
            Assert.That(equalsNull, Is.False);
            Assert.That(equalsOtherType, Is.False);
        });
    }

    [TestCase(nameof(TimeRecord.Date))]
    [TestCase(nameof(TimeRecord.Start))]
    [TestCase(nameof(TimeRecord.End))]
    [TestCase(nameof(TimeRecord.AllOvertime))]
    [TestCase(nameof(TimeRecord.Comment))]
    [TestCase(nameof(TimeRecord.TravelActive))]
    [TestCase(nameof(TimeRecord.TravelPassive))]
    public void RecordsDifferingInOneValueAreNotEqual(string property)
    {
        var target = Sample();
        var other = Sample();

        switch (property)
        {
            case nameof(TimeRecord.Date): other.Date = other.Date.AddDays(1); break;
            case nameof(TimeRecord.Start): other.Start = other.Start.AddMinutes(1); break;
            case nameof(TimeRecord.End): other.End = other.End.AddMinutes(1); break;
            case nameof(TimeRecord.AllOvertime): other.AllOvertime = true; break;
            case nameof(TimeRecord.Comment): other.Comment = "other"; break;
            case nameof(TimeRecord.TravelActive): other.TravelActive = true; break;
            case nameof(TimeRecord.TravelPassive): other.TravelPassive = false; break;
        }

        Assert.Multiple(() =>
        {
            Assert.That(target.Equals((object)other), Is.False);
            Assert.That(target.GetHashCode(), Is.Not.EqualTo(other.GetHashCode()));
        });
    }

    [TestCase("2024-02-05", "02.2024")]
    [TestCase("0999-12-31", "12.0999")]
    public void DayMonthTests(string date, string expectedMonth)
    {
        var target = new TimeRecordByDay { Date = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture) };

        Assert.That(target.Month, Is.EqualTo(expectedMonth));
    }
}
