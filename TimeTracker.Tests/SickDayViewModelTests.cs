using TimeTracker.ViewModels;

namespace TimeTracker.Tests;

public class SickDayViewModelTests
{
    [Test]
    public void DefaultsToTodayAndHalfOfEightHours()
    {
        var target = new SickDayViewModel();

        Assert.Multiple(() =>
        {
            Assert.That(target.Date, Is.EqualTo(DateOnly.FromDateTime(DateTime.Now)));
            Assert.That(target.Hours, Is.EqualTo(4));
            Assert.That(target.IsOk, Is.False);
        });
    }

    [Test]
    public void HoursFollowTheDate()
    {
        var target = new SickDayViewModel(date => date < new DateOnly(2026, 12, 1) ? 4 : 3.75m);
        var changed = new List<string?>();
        target.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        target.Date = new DateOnly(2026, 11, 30);
        var before = target.Hours;
        target.Date = new DateOnly(2026, 12, 7);

        Assert.Multiple(() =>
        {
            Assert.That(before, Is.EqualTo(4));
            Assert.That(target.Hours, Is.EqualTo(3.75m));
            Assert.That(changed, Does.Contain(nameof(SickDayViewModel.Hours)).And.Contain(nameof(SickDayViewModel.Warning)));
        });
    }

    [TestCase("2026-12-04", false, Description = "Friday")]
    [TestCase("2026-12-05", true, Description = "Saturday")]
    [TestCase("2026-12-06", true, Description = "Sunday")]
    public void WeekendIsWarned(string date, bool expectWarning)
    {
        var target = new SickDayViewModel { Date = DateOnly.Parse(date) };

        Assert.That(target.Warning, expectWarning ? Does.Contain("Weekend") : Is.Null);
    }

    [Test]
    public void CreatesSickRecordOfDateAndHours()
    {
        var target = new SickDayViewModel(_ => 3.75m) { Date = new DateOnly(2026, 12, 7) };

        var record = target.CreateRecord();

        Assert.Multiple(() =>
        {
            Assert.That(record.IsSick, Is.True);
            Assert.That(record.Date, Is.EqualTo(new DateOnly(2026, 12, 7)));
            Assert.That(record.Duration, Is.EqualTo(TimeSpan.FromHours(3.75)));
        });
    }
}
