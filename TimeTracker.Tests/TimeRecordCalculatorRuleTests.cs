using System.Globalization;
using TimeTracker.Controller;
using TimeTracker.Models;

namespace TimeTracker.Tests;

/// <summary>
///     Verifies each calculation rule in isolation with small hand made data sets.
///     Dates are ISO formatted, e.g. 2024-02-05 is a Monday.
/// </summary>
public class TimeRecordCalculatorRuleTests
{
    private readonly TimeRecordCalculator _target = new();

    private static DateOnly Date(string date)
    {
        return DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static TimeOnly Time(string time)
    {
        return TimeOnly.ParseExact(time, "HH:mm", CultureInfo.InvariantCulture);
    }

    private static TimeRecord Record(string date, string start, string end, bool travel = false, bool allOvertime = false, string? comment = null)
    {
        return new TimeRecord
        {
            Date = Date(date),
            Start = Time(start),
            End = Time(end),
            Travel = travel,
            AllOvertime = allOvertime,
            Comment = comment
        };
    }

    private static TimeRecordByDay Day(string date, decimal overtimeMinutes)
    {
        return new TimeRecordByDay { Date = Date(date), OvertimeMinutes = overtimeMinutes };
    }

    private TimeRecordByDay SingleDay(params TimeRecord[] records)
    {
        var result = _target.CalculateByDay(records);
        Assert.That(result, Has.Length.EqualTo(1));
        return result[0];
    }

    #region CalculateByDay

    [TestCase("08:00", "16:00", 0)]
    [TestCase("08:00", "15:00", -60)]
    [TestCase("08:00", "17:30", 90)]
    public void WeekdayOvertimeIsTimeBeyondEightHours(string start, string end, decimal expected)
    {
        var day = SingleDay(Record("2024-02-05", start, end));

        Assert.That(day.OvertimeMinutes, Is.EqualTo(expected));
    }

    [TestCase("2024-02-10")] // Saturday
    [TestCase("2024-02-11")] // Sunday
    public void WeekendTimeIsCompletelyOvertime(string date)
    {
        var day = SingleDay(
            Record(date, "08:00", "10:00"),
            Record(date, "10:00", "11:00", allOvertime: true));

        Assert.That(day.OvertimeMinutes, Is.EqualTo(180));
    }

    [Test]
    public void WeekdayAllOvertimeIsAddedToNormalOvertime()
    {
        // 9h normal (+60) and 1h forced overtime (+60)
        var day = SingleDay(
            Record("2024-02-05", "07:00", "16:00"),
            Record("2024-02-05", "16:00", "17:00", allOvertime: true));

        Assert.That(day.OvertimeMinutes, Is.EqualTo(120));
    }

    [Test]
    public void WeekdayWithOnlyAllOvertimeHasNoEightHoursDeduction()
    {
        var day = SingleDay(Record("2024-02-05", "18:00", "20:00", allOvertime: true));

        Assert.That(day.OvertimeMinutes, Is.EqualTo(120));
    }

    [Test]
    public void WeekdayWithAllOvertimeAndAnyNormalTimeHasEightHoursDeduction()
    {
        // As soon as there is normal time, the 8 hours are deducted from it
        var day = SingleDay(
            Record("2024-02-05", "18:00", "20:00", allOvertime: true),
            Record("2024-02-05", "08:00", "08:01"));

        Assert.That(day.OvertimeMinutes, Is.EqualTo(120 + 1 - 480));
    }

    [Test]
    public void TravelWithAllOvertimeIsCountedOnce()
    {
        var day = SingleDay(
            Record("2024-02-05", "08:00", "16:00"),
            Record("2024-02-05", "16:00", "18:00", travel: true, allOvertime: true));

        Assert.Multiple(() =>
        {
            Assert.That(day.OvertimeMinutes, Is.EqualTo(120));
            Assert.That(day.WorkMinutes, Is.EqualTo(480));
            Assert.That(day.TravelMinutes, Is.EqualTo(120));
        });
    }

    [Test]
    public void TravelCountsAsWorkingTime()
    {
        var day = SingleDay(Record("2024-02-05", "07:00", "17:00", travel: true));

        Assert.Multiple(() =>
        {
            Assert.That(day.OvertimeMinutes, Is.EqualTo(120));
            Assert.That(day.WorkMinutes, Is.Zero);
            Assert.That(day.TravelMinutes, Is.EqualTo(600));
        });
    }

    [Test]
    public void DaysWithoutRecordsAreNotCalculated()
    {
        var result = _target.CalculateByDay([
            Record("2024-02-05", "08:00", "16:00"),
            Record("2024-02-07", "08:00", "16:00")
        ]);

        Assert.That(result.Select(_ => _.Date), Is.EquivalentTo(new[] { Date("2024-02-05"), Date("2024-02-07") }));
    }

    [Test]
    public void WorkAndTravelMinutesAreSummed()
    {
        var day = SingleDay(
            Record("2024-02-05", "06:00", "07:30", travel: true),
            Record("2024-02-05", "08:00", "12:00"),
            Record("2024-02-05", "12:30", "16:15"),
            Record("2024-02-05", "17:00", "18:00", travel: true));

        Assert.Multiple(() =>
        {
            Assert.That(day.WorkMinutes, Is.EqualTo(4 * 60 + 3 * 60 + 45));
            Assert.That(day.TravelMinutes, Is.EqualTo(90 + 60));
        });
    }

    [Test]
    public void StartIsFirstWorkStartEvenIfTravelStartsEarlier()
    {
        var day = SingleDay(
            Record("2024-02-05", "06:00", "08:00", travel: true),
            Record("2024-02-05", "08:30", "12:00"));

        Assert.That(day.Start, Is.EqualTo(Time("08:30")));
    }

    [Test]
    public void StartIsFirstTravelStartOnTravelOnlyDay()
    {
        var day = SingleDay(
            Record("2024-02-05", "09:00", "11:00", travel: true),
            Record("2024-02-05", "05:00", "07:00", travel: true));

        Assert.That(day.Start, Is.EqualTo(Time("05:00")));
    }

    [Test]
    public void StartDoesNotDependOnRecordOrder()
    {
        var day = SingleDay(
            Record("2024-02-05", "13:00", "15:00"),
            Record("2024-02-05", "08:00", "12:00"));

        Assert.That(day.Start, Is.EqualTo(Time("08:00")));
    }

    #endregion

    #region CalculateCategorySummary

    [Test]
    public void CategoriesAreGroupedIgnoringCase()
    {
        var result = _target.CalculateCategorySummary([
            Record("2024-02-05", "08:00", "09:00", comment: "duna"),
            Record("2024-02-05", "09:00", "09:30", comment: "DUNA: meeting")
        ]);

        Assert.That(result, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Category, Is.EqualTo("Duna"), "Name of the first record is used");
            Assert.That(result[0].WorkMinutes, Is.EqualTo(90));
        });
    }

    [Test]
    public void CategoryTimeIsSplitIntoWorkAndTravel()
    {
        var result = _target.CalculateCategorySummary([
            Record("2024-02-05", "08:00", "10:00", comment: "DUNA"),
            Record("2024-02-05", "06:00", "07:00", travel: true, comment: "DUNA: Travel")
        ]);

        Assert.That(result, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].WorkMinutes, Is.EqualTo(120));
            Assert.That(result[0].TravelMinutes, Is.EqualTo(60));
            Assert.That(result[0].Duration, Is.EqualTo(TimeSpan.FromHours(3)));
        });
    }

    [Test]
    public void SameCategoryOnDifferentDaysIsNotSummarized()
    {
        var result = _target.CalculateCategorySummary([
            Record("2024-02-05", "08:00", "10:00", comment: "DUNA"),
            Record("2024-02-06", "08:00", "10:00", comment: "DUNA")
        ]);

        Assert.That(result, Has.Length.EqualTo(2));
    }

    [Test]
    public void GmkCommentsAreNotSummarized()
    {
        var result = _target.CalculateCategorySummary([
            Record("2024-02-05", "08:00", "10:00", comment: "GMK: a"),
            Record("2024-02-05", "10:00", "11:00", comment: "GMK: b")
        ]);

        Assert.That(result.Select(_ => _.Category), Is.EquivalentTo(new[] { "GMK: a", "GMK: b" }));
    }

    [Test]
    public void EmptyCommentIsEmptyCategory()
    {
        var result = _target.CalculateCategorySummary([
            Record("2024-02-05", "08:00", "10:00"),
            Record("2024-02-05", "10:00", "11:00", comment: " ")
        ]);

        Assert.That(result, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Category, Is.Empty);
            Assert.That(result[0].WorkMinutes, Is.EqualTo(180));
        });
    }

    #endregion

    #region CalculateByMonth / CalculateTotalOvertime

    [Test]
    public void SameMonthOfDifferentYearsIsNotSummarized()
    {
        var result = _target.CalculateByMonth([Day("2023-01-10", 10), Day("2024-01-10", 20), Day("2024-01-11", 5)]);

        Assert.That(result.Select(_ => (_.MonthDisplay, _.OvertimeMinutes)),
            Is.EquivalentTo(new[] { ("01.2023", 10m), ("01.2024", 25m) }));
    }

    [Test]
    public void EmptyDataHasNoMonthsAndNoOvertime()
    {
        var months = _target.CalculateByMonth([]);

        Assert.Multiple(() =>
        {
            Assert.That(months, Is.Empty);
            Assert.That(_target.CalculateTotalOvertime(months), Is.Zero);
        });
    }

    [Test]
    public void TotalOvertimeIsSumOfAllDays()
    {
        TimeRecordByDay[] days = [Day("2024-01-31", 30), Day("2024-02-01", -45), Day("2024-03-15", 100)];

        var total = _target.CalculateTotalOvertime(_target.CalculateByMonth(days));

        Assert.That(total, Is.EqualTo(days.Sum(_ => _.OvertimeMinutes)));
    }

    #endregion

    #region CalculateWeeklyTotals / CalculateRunningTotals

    [Test]
    public void WeeklyTotalStartsOnMonday()
    {
        TimeRecordByDay[] days = [Day("2024-02-09", 60), Day("2024-02-12", 30), Day("2024-02-13", -10)];

        _target.CalculateWeeklyTotals(days);

        Assert.Multiple(() =>
        {
            Assert.That(days.Select(_ => _.OvertimeThisWeek.TotalMinutes), Is.EqualTo(new[] { 60, 30, 20 }));
            Assert.That(days.Select(_ => _.IsLastOfWeek), Is.EqualTo(new[] { true, false, true }));
        });
    }

    [Test]
    public void WeeklyTotalStartsNewWeekWithoutMondayRecord()
    {
        // Monday 2024-02-12 has no record, e.g. holiday
        TimeRecordByDay[] days = [Day("2024-02-08", 15), Day("2024-02-09", 60), Day("2024-02-13", 30)];

        _target.CalculateWeeklyTotals(days);

        Assert.Multiple(() =>
        {
            Assert.That(days.Select(_ => _.OvertimeThisWeek.TotalMinutes), Is.EqualTo(new[] { 15, 75, 30 }));
            Assert.That(days.Select(_ => _.IsLastOfWeek), Is.EqualTo(new[] { false, true, true }));
        });
    }

    [Test]
    public void WeekendBelongsToWeekOfPrecedingMonday()
    {
        TimeRecordByDay[] days = [Day("2024-02-09", 60), Day("2024-02-11", 120)];

        _target.CalculateWeeklyTotals(days);

        Assert.Multiple(() =>
        {
            Assert.That(days.Select(_ => _.OvertimeThisWeek.TotalMinutes), Is.EqualTo(new[] { 60, 180 }));
            Assert.That(days.Select(_ => _.IsLastOfWeek), Is.EqualTo(new[] { false, true }));
        });
    }

    [Test]
    public void WeeklyTotalSpansTurnOfTheYear()
    {
        // Monday 2024-12-30 to Sunday 2025-01-05 is one week
        TimeRecordByDay[] days = [Day("2024-12-30", 60), Day("2025-01-03", 30), Day("2025-01-06", 10)];

        _target.CalculateWeeklyTotals(days);

        Assert.Multiple(() =>
        {
            Assert.That(days.Select(_ => _.OvertimeThisWeek.TotalMinutes), Is.EqualTo(new[] { 60, 90, 10 }));
            Assert.That(days.Select(_ => _.IsLastOfWeek), Is.EqualTo(new[] { false, true, true }));
        });
    }

    [Test]
    public void WeeklyTotalsOfEmptyDataDoNotFail()
    {
        Assert.DoesNotThrow(() => _target.CalculateWeeklyTotals([]));
    }

    [Test]
    public void RunningTotalAccumulatesMonths()
    {
        var months = _target.CalculateByMonth([Day("2024-01-10", 60), Day("2024-02-10", -90), Day("2024-03-10", 10)]);

        _target.CalculateRunningTotals(months);

        Assert.Multiple(() =>
        {
            Assert.That(months.Select(_ => _.OvertimeTotal.TotalMinutes), Is.EqualTo(new[] { 60, -30, -20 }));
            Assert.That(months[^1].OvertimeTotal.TotalMinutes, Is.EqualTo((double)_target.CalculateTotalOvertime(months)));
        });
    }

    #endregion

    #region CalculateExcel

    private TimeRecordExcel SingleExcel(string start, decimal workMinutes, decimal travelMinutes)
    {
        var day = new TimeRecordByDay
        {
            Date = Date("2024-02-05"),
            Start = Time(start),
            WorkMinutes = workMinutes,
            TravelMinutes = travelMinutes
        };

        var result = _target.CalculateExcel([day]);
        Assert.That(result, Has.Length.EqualTo(1));
        return result[0];
    }

    [Test]
    public void ExcelWorkOnlyAddsBreakToEnd()
    {
        var result = SingleExcel("08:00", 480, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Start, Is.EqualTo(Time("08:00")));
            Assert.That(result.End, Is.EqualTo(Time("16:45")));
            Assert.That(result.TravelStart, Is.Null);
            Assert.That(result.TravelEnd, Is.Null);
        });
    }

    [Test]
    public void ExcelTravelOnlyHasNoWorkTime()
    {
        var result = SingleExcel("06:00", 0, 120);

        Assert.Multiple(() =>
        {
            Assert.That(result.Start, Is.Null);
            Assert.That(result.End, Is.Null);
            Assert.That(result.TravelStart, Is.EqualTo(Time("06:00")));
            Assert.That(result.TravelEnd, Is.EqualTo(Time("08:00")));
        });
    }

    [Test]
    public void ExcelTravelIsSplitAroundWorkTime()
    {
        var result = SingleExcel("08:00", 480, 120);

        Assert.Multiple(() =>
        {
            Assert.That(result.Start, Is.EqualTo(Time("08:00")));
            Assert.That(result.End, Is.EqualTo(Time("16:45")));
            Assert.That(result.TravelStart, Is.EqualTo(Time("07:00")));
            Assert.That(result.TravelEnd, Is.EqualTo(Time("17:45")));
        });
    }

    [Test]
    public void ExcelOddTravelMinutesResultInSeconds()
    {
        var result = SingleExcel("08:00", 480, 45);

        Assert.Multiple(() =>
        {
            Assert.That(result.TravelStart, Is.EqualTo(new TimeOnly(7, 37, 30)));
            Assert.That(result.TravelEnd, Is.EqualTo(new TimeOnly(17, 7, 30)));
        });
    }

    [Test]
    public void ExcelDayWithoutDurationOnlyContainsBreak()
    {
        // Documents current behavior: a day without any time is treated as working day
        var result = SingleExcel("08:00", 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Start, Is.EqualTo(Time("08:00")));
            Assert.That(result.End, Is.EqualTo(Time("08:45")));
            Assert.That(result.TravelStart, Is.Null);
        });
    }

    [Test]
    public void ExcelEndWrapsAroundMidnight()
    {
        var result = SingleExcel("20:00", 300, 0);

        Assert.That(result.End, Is.EqualTo(Time("01:45")));
    }

    #endregion
}
