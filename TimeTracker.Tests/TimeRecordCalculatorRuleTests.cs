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

    private static TimeRecord Record(string date, string start, string end, bool travel = false, bool allOvertime = false, string? comment = null, bool passive = false)
    {
        return new TimeRecord
        {
            Date = Date(date),
            Start = Time(start),
            End = Time(end),
            TravelActive = travel,
            TravelPassive = passive,
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
            Assert.That(day.TravelActiveMinutes, Is.EqualTo(120));
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
            Assert.That(day.TravelActiveMinutes, Is.EqualTo(600));
        });
    }

    [Test]
    public void PassiveTravelCountsAsWorkingTime()
    {
        var day = SingleDay(Record("2024-02-05", "07:00", "17:00", passive: true));

        Assert.Multiple(() =>
        {
            Assert.That(day.OvertimeMinutes, Is.EqualTo(120));
            Assert.That(day.WorkMinutes, Is.Zero);
            Assert.That(day.TravelActiveMinutes, Is.Zero);
            Assert.That(day.TravelPassiveMinutes, Is.EqualTo(600));
        });
    }

    [Test]
    public void WorkAndPassiveTravelResultInTotalWorkingTime()
    {
        // Monday: 7:15 h work and 6 h passive travel
        var records = new[]
        {
            Record("2026-11-16", "08:00", "15:15"),
            Record("2026-11-16", "15:15", "21:15", passive: true)
        };

        var day = SingleDay(records);
        var category = _target.CalculateCategorySummary(records);

        Assert.Multiple(() =>
        {
            Assert.That(day.WorkMinutes, Is.EqualTo(7 * 60 + 15));
            Assert.That(day.TravelPassiveMinutes, Is.EqualTo(6 * 60));
            Assert.That(day.WorkMinutes + day.TravelActiveMinutes + day.TravelPassiveMinutes, Is.EqualTo(13 * 60 + 15), "Total time");
            Assert.That(category.Single().Duration, Is.EqualTo(new TimeSpan(13, 15, 0)), "Total time of the category");
            Assert.That(day.Overtime, Is.EqualTo(new TimeSpan(5, 15, 0)), "Overtime beyond eight hours");
        });
    }

    [Test]
    public void ActiveAndPassiveTravelAreSummedSeparately()
    {
        var day = SingleDay(
            Record("2024-02-05", "06:00", "07:00", travel: true),
            Record("2024-02-05", "07:00", "08:00", passive: true),
            Record("2024-02-05", "08:00", "15:00"),
            Record("2024-02-05", "15:00", "17:30", passive: true));

        Assert.Multiple(() =>
        {
            Assert.That(day.WorkMinutes, Is.EqualTo(420));
            Assert.That(day.TravelActiveMinutes, Is.EqualTo(60));
            Assert.That(day.TravelPassiveMinutes, Is.EqualTo(60 + 150));
            Assert.That(day.OvertimeMinutes, Is.EqualTo(420 + 60 + 210 - 480));
            Assert.That(day.Start, Is.EqualTo(Time("08:00")), "Start is the first work start");
        });
    }

    [Test]
    public void PassiveTravelWithAllOvertimeIsCountedOnce()
    {
        var day = SingleDay(
            Record("2024-02-05", "08:00", "16:00"),
            Record("2024-02-05", "16:00", "18:00", allOvertime: true, passive: true));

        Assert.Multiple(() =>
        {
            Assert.That(day.OvertimeMinutes, Is.EqualTo(120));
            Assert.That(day.TravelPassiveMinutes, Is.EqualTo(120));
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
            Assert.That(day.TravelActiveMinutes, Is.EqualTo(90 + 60));
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

    #region Flextime

    [TestCase("2024-02-05", 8, -480)]
    [TestCase("2024-02-05", 3.5, -210)]
    [TestCase("2024-02-10", 4, -240)] // Saturday
    public void FlextimeOnlyDayReducesOvertimeByHours(string date, decimal hours, decimal expected)
    {
        var day = SingleDay(TimeRecord.CreateFlextime(Date(date), hours));

        Assert.Multiple(() =>
        {
            Assert.That(day.OvertimeMinutes, Is.EqualTo(expected));
            Assert.That(day.FlextimeMinutes, Is.EqualTo(hours * 60));
            Assert.That(day.WorkMinutes, Is.Zero);
            Assert.That(day.TravelActiveMinutes, Is.Zero);
        });
    }

    [Test]
    public void FlextimeIsIgnoredOnDaysWithWorkingTime()
    {
        // Half day off: the missing 4 hours already result from the working time
        var day = SingleDay(
            Record("2024-02-05", "08:00", "12:00"),
            TimeRecord.CreateFlextime(Date("2024-02-05"), 4));

        Assert.Multiple(() =>
        {
            Assert.That(day.OvertimeMinutes, Is.EqualTo(-240));
            Assert.That(day.WorkMinutes, Is.EqualTo(240));
            Assert.That(day.Start, Is.EqualTo(Time("08:00")));
        });
    }

    [Test]
    public void FlextimeWithCommentIsRecognized()
    {
        var day = SingleDay(Record("2024-02-05", "00:00", "02:00", comment: "gleitzeit: Arzt"));

        Assert.That(day.OvertimeMinutes, Is.EqualTo(-120));
    }

    [Test]
    public void ZeroDurationDayOffStillCountsEightHours()
    {
        // Existing data uses "Gleittag" with start = end, which is a normal record
        var day = SingleDay(Record("2024-04-03", "07:38", "07:38", comment: "Gleittag"));

        Assert.That(day.OvertimeMinutes, Is.EqualTo(-480));
    }

    [Test]
    public void ExcelSkipsFlextimeOnlyDays()
    {
        var days = _target.CalculateByDay([
            TimeRecord.CreateFlextime(Date("2024-02-05"), 8),
            Record("2024-02-06", "08:00", "12:00"),
            TimeRecord.CreateFlextime(Date("2024-02-06"), 4)
        ]);

        var result = _target.CalculateExcel(days);

        Assert.That(result.Select(_ => _.Date), Is.EqualTo(new[] { Date("2024-02-06") }));
    }

    [Test]
    public void CreateFlextimeStoresHoursAsDurationFromMidnight()
    {
        var record = TimeRecord.CreateFlextime(Date("2024-02-05"), 7.5m);

        Assert.Multiple(() =>
        {
            Assert.That(record.Start, Is.EqualTo(TimeOnly.MinValue));
            Assert.That(record.End, Is.EqualTo(Time("07:30")));
            Assert.That(record.Comment, Is.EqualTo("Gleitzeit"));
            Assert.That(record.IsFlextime, Is.True);
        });
    }

    [Test]
    public void FlextimeSurvivesCsvRoundTrip()
    {
        var serializer = new TimeRecordSerializer();
        var csv = serializer.Serialize([TimeRecord.CreateFlextime(Date("2024-02-05"), 8)]);

        var result = serializer.Deserialize(csv);

        Assert.Multiple(() =>
        {
            Assert.That(csv, Is.EqualTo("05.02.2024;00:00;08:00;;Gleitzeit;;\n"));
            Assert.That(result.Single().IsFlextime, Is.True);
            Assert.That(result.Single().Duration, Is.EqualTo(TimeSpan.FromHours(8)));
        });
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
            Assert.That(result[0].TravelActiveMinutes, Is.EqualTo(60));
            Assert.That(result[0].Duration, Is.EqualTo(TimeSpan.FromHours(3)));
        });
    }

    [Test]
    public void CategoryTimeIsSplitIntoWorkActiveAndPassiveTravel()
    {
        var result = _target.CalculateCategorySummary([
            Record("2024-02-05", "08:00", "10:00", comment: "DUNA"),
            Record("2024-02-05", "06:00", "07:00", travel: true, comment: "DUNA: Travel"),
            Record("2024-02-05", "10:00", "10:30", comment: "DUNA: Train", passive: true)
        ]);

        Assert.That(result, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].WorkMinutes, Is.EqualTo(120));
            Assert.That(result[0].TravelActiveMinutes, Is.EqualTo(60));
            Assert.That(result[0].TravelPassiveMinutes, Is.EqualTo(30));
            Assert.That(result[0].Duration, Is.EqualTo(TimeSpan.FromHours(3.5)));
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

    #region Rules since 01.12.2026

    // 2026-12-07 is a Monday, 2026-12-05 a Saturday, 2026-12-06 a Sunday

    [Test]
    public void Rules2026WorkAndPassiveTravelUpToSixHoursCountFully()
    {
        // Same scenario as WorkAndPassiveTravelResultInTotalWorkingTime, but with the new rules
        var day = SingleDay(
            Record("2026-12-07", "08:00", "15:15"),
            Record("2026-12-07", "15:15", "21:15", passive: true));

        Assert.Multiple(() =>
        {
            Assert.That(day.Overtime, Is.EqualTo(new TimeSpan(5, 15, 0)), "13:15 h total minus 8 h target");
            Assert.That(day.HasLoss, Is.False);
            Assert.That(day.Loss, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    public void Rules2026PassiveTravelBeyondSixHoursCountsHalfOnDaysWithWork()
    {
        var day = SingleDay(
            Record("2026-12-07", "08:00", "15:15"),
            Record("2026-12-07", "15:15", "23:15", passive: true));

        Assert.Multiple(() =>
        {
            // 7:15 h work + 6 h passive travel + 2 h * 0.5 = 14:15 h
            Assert.That(day.Overtime, Is.EqualTo(new TimeSpan(6, 15, 0)));
            Assert.That(day.PassiveTravelLossMinutes, Is.EqualTo(60));
            Assert.That(day.WorkCapLossMinutes, Is.Zero);
            Assert.That(day.Loss, Is.EqualTo(TimeSpan.FromHours(1)));
            Assert.That(day.LossDescription, Does.Contain("Travel (passive)"));
        });
    }

    [Test]
    public void Rules2026PassiveTravelOnlyIsLimitedToTargetHours()
    {
        var day = SingleDay(Record("2026-12-07", "06:00", "16:00", passive: true));

        Assert.Multiple(() =>
        {
            Assert.That(day.OvertimeMinutes, Is.Zero);
            Assert.That(day.PassiveTravelLossMinutes, Is.EqualTo(120));
            Assert.That(day.IsOverMaxWorkTime, Is.False);
        });
    }

    [Test]
    public void Rules2026PassiveTravelOnlyBelowTargetHasNoLoss()
    {
        var day = SingleDay(Record("2026-12-07", "06:00", "12:00", passive: true));

        Assert.Multiple(() =>
        {
            Assert.That(day.OvertimeMinutes, Is.EqualTo(-120));
            Assert.That(day.HasLoss, Is.False);
        });
    }

    [Test]
    public void Rules2026PassiveTravelLimitUsesConfiguredTargetHours()
    {
        var target = new TimeRecordCalculator(7);

        var passiveOnly = target.CalculateByDay([Record("2026-12-07", "06:00", "16:00", passive: true)]).Single();
        var workOnly = target.CalculateByDay([Record("2026-12-08", "08:00", "16:00")]).Single();

        Assert.Multiple(() =>
        {
            Assert.That(passiveOnly.OvertimeMinutes, Is.Zero);
            Assert.That(passiveOnly.PassiveTravelLossMinutes, Is.EqualTo(180));
            Assert.That(workOnly.OvertimeMinutes, Is.EqualTo(60));
        });
    }

    [Test]
    public void Rules2026WorkIsLimitedToTenHours()
    {
        var day = SingleDay(Record("2026-12-07", "07:00", "18:00"));

        Assert.Multiple(() =>
        {
            Assert.That(day.OvertimeMinutes, Is.EqualTo(120));
            Assert.That(day.WorkCapLossMinutes, Is.EqualTo(60));
            Assert.That(day.IsOverMaxWorkTime, Is.True);
            Assert.That(day.LossDescription, Does.Contain("over 10:00 h"));
        });
    }

    [Test]
    public void Rules2026ActiveTravelIsPartOfTheTenHours()
    {
        var day = SingleDay(
            Record("2026-12-07", "05:00", "08:00", travel: true),
            Record("2026-12-07", "08:00", "16:00"));

        Assert.Multiple(() =>
        {
            Assert.That(day.OvertimeMinutes, Is.EqualTo(120));
            Assert.That(day.WorkCapLossMinutes, Is.EqualTo(60));
        });
    }

    [Test]
    public void Rules2026PassiveTravelIsNotPartOfTheTenHours()
    {
        var day = SingleDay(
            Record("2026-12-07", "08:00", "18:00"),
            Record("2026-12-07", "18:00", "20:00", passive: true));

        Assert.Multiple(() =>
        {
            Assert.That(day.OvertimeMinutes, Is.EqualTo(240));
            Assert.That(day.HasLoss, Is.False);
        });
    }

    [TestCase("2026-12-05", "08:00", "12:00", 300, Description = "Saturday +25%")]
    [TestCase("2026-12-06", "08:00", "12:00", 480, Description = "Sunday +100%")]
    [TestCase("2026-12-05", "07:00", "18:00", 750, Description = "Saturday, limited to 10 h before the surcharge")]
    public void Rules2026WeekendWorkGetsSurcharge(string date, string start, string end, decimal expectedOvertime)
    {
        var day = SingleDay(Record(date, start, end));

        Assert.That(day.OvertimeMinutes, Is.EqualTo(expectedOvertime));
    }

    [Test]
    public void Rules2026WeekendSurchargeAppliesToActiveButNotToPassiveTravel()
    {
        var day = SingleDay(
            Record("2026-12-06", "08:00", "10:00", travel: true),
            Record("2026-12-06", "10:00", "12:00"),
            Record("2026-12-06", "12:00", "14:00", passive: true));

        Assert.Multiple(() =>
        {
            // (2 h + 2 h) * 2 + 2 h
            Assert.That(day.OvertimeMinutes, Is.EqualTo(600));
            Assert.That(day.SurchargeMinutes, Is.EqualTo(240));
        });
    }

    [Test]
    public void Rules2026AllOvertimeIsNotComparedToTarget()
    {
        var day = SingleDay(
            Record("2026-12-07", "08:00", "16:00"),
            Record("2026-12-07", "16:00", "19:00", allOvertime: true));

        Assert.Multiple(() =>
        {
            // 11 h are limited to 10 h, 3 h of them are all overtime
            Assert.That(day.OvertimeMinutes, Is.EqualTo(180 + (420 - 480)));
            Assert.That(day.WorkCapLossMinutes, Is.EqualTo(60));
        });
    }

    [Test]
    public void Rules2026StartOn1stDecember()
    {
        var result = _target.CalculateByDay([
            Record("2026-11-30", "07:00", "18:00"),
            Record("2026-12-01", "07:00", "18:00")
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(result[0].OvertimeMinutes, Is.EqualTo(180), "Old rules without limit");
            Assert.That(result[0].HasLoss, Is.False);
            Assert.That(result[1].OvertimeMinutes, Is.EqualTo(120), "New rules with limit to 10 h");
            Assert.That(result[1].HasLoss, Is.True);
        });
    }

    [Test]
    public void OldRulesIgnoreConfiguredTargetHours()
    {
        var day = new TimeRecordCalculator(7).CalculateByDay([Record("2026-11-30", "08:00", "16:00")]).Single();

        Assert.That(day.OvertimeMinutes, Is.Zero);
    }

    [Test]
    public void Rules2026FlextimeIsUnchanged()
    {
        var day = SingleDay(TimeRecord.CreateFlextime(Date("2026-12-07"), 8));

        Assert.Multiple(() =>
        {
            Assert.That(day.OvertimeMinutes, Is.EqualTo(-480));
            Assert.That(day.HasLoss, Is.False);
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(25)]
    public void InvalidTargetHoursAreRejected(double hours)
    {
        Assert.That(() => new TimeRecordCalculator(hours), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void FurtherRuleSetAppliesFromItsStart()
    {
        // A rule change in 2027 only needs a further rule set
        var rules = WorkingTimeRules.CreateDefault()
            .Append(new WorkingTimeRules { ValidFrom = new DateOnly(2027, 1, 1), DailyTargetMinutes = 7 * 60, MaxWorkMinutes = 9 * 60 })
            .ToArray();
        var target = new TimeRecordCalculator(rules);

        var result = target.CalculateByDay([
            Record("2026-12-31", "07:00", "18:00"),
            Record("2027-01-04", "07:00", "18:00")
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(result[0].OvertimeMinutes, Is.EqualTo(120), "Rules of 2026: limit 10 h, target 8 h");
            Assert.That(result[1].OvertimeMinutes, Is.EqualTo(120), "Rules of 2027: limit 9 h, target 7 h");
            Assert.That(result[1].WorkCapLossMinutes, Is.EqualTo(120));
            Assert.That(result[1].LossDescription, Does.Contain("over 09:00 h"));
        });
    }

    [TestCase("2000-01-01", 0)]
    [TestCase("2026-11-30", 0)]
    [TestCase("2026-12-01", 1)]
    [TestCase("2030-06-01", 1)]
    public void RuleSetIsSelectedByDate(string date, int expectedIndex)
    {
        var rules = WorkingTimeRules.CreateDefault();

        Assert.That(new TimeRecordCalculator(rules).GetRules(Date(date)), Is.SameAs(rules[expectedIndex]));
    }

    [Test]
    public void RuleSetsAreRequired()
    {
        Assert.That(() => new TimeRecordCalculator(Array.Empty<WorkingTimeRules>()), Throws.ArgumentException);
    }

    #endregion

    #region Sick

    private static TimeRecord Sick(string date, decimal hours)
    {
        return TimeRecord.CreateSick(Date(date), hours);
    }

    [Test]
    public void CreateSickStoresHoursAsDurationFromMidnight()
    {
        var record = Sick("2024-02-05", 3.75m);

        Assert.Multiple(() =>
        {
            Assert.That(record.Start, Is.EqualTo(TimeOnly.MinValue));
            Assert.That(record.Duration, Is.EqualTo(TimeSpan.FromHours(3.75)));
            Assert.That(record.Comment, Is.EqualTo(TimeRecord.SickCategory));
            Assert.That(record.IsSick, Is.True);
            Assert.That(record.IsFlextime, Is.False);
        });
    }

    [TestCase("krank")]
    [TestCase("Krank: half day")]
    public void SickIsRecognizedByCategory(string comment)
    {
        Assert.That(new TimeRecord { Comment = comment }.IsSick, Is.True);
    }

    [TestCase("08:00", "13:00", 60, Description = "5 h work + 4 h sick = 1 h overtime")]
    [TestCase("08:00", "12:00", 0)]
    [TestCase("08:00", "11:00", -60)]
    public void HalfSickDayCreditsHalfTheTarget(string start, string end, decimal expected)
    {
        var day = SingleDay(Record("2024-02-05", start, end), Sick("2024-02-05", 4));

        Assert.Multiple(() =>
        {
            Assert.That(day.OvertimeMinutes, Is.EqualTo(expected));
            Assert.That(day.SickMinutes, Is.EqualTo(240));
            Assert.That(day.WorkMinutes, Is.EqualTo((decimal)(Time(end) - Time(start)).TotalMinutes), "Sickness is not work");
            Assert.That(day.Start, Is.EqualTo(Time(start)), "Start of the sick record is ignored");
        });
    }

    [Test]
    public void HalfSickDayWithoutWorkMissesTheOtherHalf()
    {
        var day = SingleDay(Sick("2024-02-05", 4));

        Assert.That(day.OvertimeMinutes, Is.EqualTo(-240));
    }

    [Test]
    public void HalfSickDayWithFlextimeForTheOtherHalfOnlyCountsTheFlextime()
    {
        var day = SingleDay(Sick("2024-02-05", 4), TimeRecord.CreateFlextime(Date("2024-02-05"), 4));

        Assert.That(day.OvertimeMinutes, Is.EqualTo(-240));
    }

    [TestCase("2024-02-10", Description = "Saturday")]
    [TestCase("2024-02-11", Description = "Sunday")]
    public void SicknessOnWeekendIsNotCredited(string date)
    {
        var sickOnly = SingleDay(Sick(date, 4));
        var withWork = SingleDay(Record(date, "08:00", "10:00"), Sick(date, 4));

        Assert.Multiple(() =>
        {
            Assert.That(sickOnly.OvertimeMinutes, Is.Zero);
            Assert.That(withWork.OvertimeMinutes, Is.EqualTo(120));
        });
    }

    [Test]
    public void SicknessIsNotLimitedByTheMaximumWorkTime()
    {
        // Monday under the 2026 rules: 9 h work + 4 h sick, the 10 h maximum only applies to the work
        var day = SingleDay(Record("2026-12-07", "08:00", "17:00"), Sick("2026-12-07", 4));

        Assert.Multiple(() =>
        {
            Assert.That(day.WorkCapLossMinutes, Is.Zero);
            Assert.That(day.HasLoss, Is.False);
            Assert.That(day.OvertimeMinutes, Is.EqualTo(5 * 60));
        });
    }

    [Test]
    public void SicknessIsNotWorkForPassiveTravel()
    {
        // Under the 2026 rules passive travel without work is limited to the target
        var day = SingleDay(Sick("2026-12-07", 4), Record("2026-12-07", "08:00", "18:00", passive: true));

        Assert.Multiple(() =>
        {
            Assert.That(day.PassiveTravelLossMinutes, Is.EqualTo(120));
            Assert.That(day.OvertimeMinutes, Is.EqualTo(4 * 60));
        });
    }

    [Test]
    public void SicknessIsStoredAndRead()
    {
        var serializer = new TimeRecordSerializer();
        var csv = serializer.Serialize([Sick("2024-02-05", 4)]);

        var result = serializer.Deserialize(csv);

        Assert.Multiple(() =>
        {
            Assert.That(csv, Is.EqualTo("05.02.2024;00:00;04:00;;Krank;;\n"));
            Assert.That(result.Single().IsSick, Is.True);
        });
    }

    [Test]
    public void ExcelSkipsSickOnlyDaysAndIgnoresSickness()
    {
        var days = _target.CalculateByDay([
            Sick("2024-02-05", 4),
            Record("2024-02-06", "08:00", "12:00"), Sick("2024-02-06", 4)
        ]);

        var result = _target.CalculateExcel(days);

        Assert.Multiple(() =>
        {
            Assert.That(result.Select(_ => _.Date), Is.EqualTo(new[] { Date("2024-02-06") }));
            Assert.That(result[0].Start, Is.EqualTo(Time("08:00")));
            Assert.That(result[0].End, Is.EqualTo(Time("12:45")), "Work and break only");
        });
    }

    #endregion

    #region CalculateExcel

    private TimeRecordExcel SingleExcel(string start, decimal workMinutes, decimal travelMinutes, decimal passiveMinutes = 0)
    {
        var day = new TimeRecordByDay
        {
            Date = Date("2024-02-05"),
            Start = Time(start),
            WorkMinutes = workMinutes,
            TravelActiveMinutes = travelMinutes,
            TravelPassiveMinutes = passiveMinutes
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
            Assert.That(result.TravelActiveStart, Is.Null);
            Assert.That(result.TravelActiveEnd, Is.Null);
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
            Assert.That(result.TravelActiveStart, Is.EqualTo(Time("06:00")));
            Assert.That(result.TravelActiveEnd, Is.EqualTo(Time("08:00")));
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
            Assert.That(result.TravelActiveStart, Is.EqualTo(Time("07:00")));
            Assert.That(result.TravelActiveEnd, Is.EqualTo(Time("17:45")));
        });
    }

    [Test]
    public void ExcelPassiveTravelIsSplitAroundWorkTime()
    {
        var result = SingleExcel("08:00", 480, 0, 120);

        Assert.Multiple(() =>
        {
            Assert.That(result.TravelActiveStart, Is.Null);
            Assert.That(result.TravelActiveEnd, Is.Null);
            Assert.That(result.TravelPassiveStart, Is.EqualTo(Time("07:00")));
            Assert.That(result.TravelPassiveEnd, Is.EqualTo(Time("17:45")));
        });
    }

    [Test]
    public void ExcelPassiveTravelIsSplitAroundActiveTravel()
    {
        var result = SingleExcel("08:00", 480, 120, 60);

        Assert.Multiple(() =>
        {
            Assert.That(result.Start, Is.EqualTo(Time("08:00")));
            Assert.That(result.End, Is.EqualTo(Time("16:45")));
            Assert.That(result.TravelActiveStart, Is.EqualTo(Time("07:00")));
            Assert.That(result.TravelActiveEnd, Is.EqualTo(Time("17:45")));
            Assert.That(result.TravelPassiveStart, Is.EqualTo(Time("06:30")));
            Assert.That(result.TravelPassiveEnd, Is.EqualTo(Time("18:15")));
        });
    }

    [Test]
    public void ExcelPassiveTravelOnlyHasNoWorkTime()
    {
        var result = SingleExcel("06:00", 0, 0, 120);

        Assert.Multiple(() =>
        {
            Assert.That(result.Start, Is.Null);
            Assert.That(result.End, Is.Null);
            Assert.That(result.TravelActiveStart, Is.Null);
            Assert.That(result.TravelPassiveStart, Is.EqualTo(Time("06:00")));
            Assert.That(result.TravelPassiveEnd, Is.EqualTo(Time("08:00")));
        });
    }

    [Test]
    public void ExcelOddTravelMinutesResultInSeconds()
    {
        var result = SingleExcel("08:00", 480, 45);

        Assert.Multiple(() =>
        {
            Assert.That(result.TravelActiveStart, Is.EqualTo(new TimeOnly(7, 37, 30)));
            Assert.That(result.TravelActiveEnd, Is.EqualTo(new TimeOnly(17, 7, 30)));
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
            Assert.That(result.TravelActiveStart, Is.Null);
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
