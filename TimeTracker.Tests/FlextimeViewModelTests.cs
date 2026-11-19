using TimeTracker.ViewModels;

namespace TimeTracker.Tests;

public class FlextimeViewModelTests
{
    [Test]
    public void DefaultsToTodayAndEightHours()
    {
        var target = new FlextimeViewModel();

        Assert.Multiple(() =>
        {
            Assert.That(target.Date, Is.EqualTo(DateOnly.FromDateTime(DateTime.Now)));
            Assert.That(target.Hours, Is.EqualTo(FlextimeViewModel.DefaultHours));
            Assert.That(target.IsOk, Is.False);
        });
    }

    [Test]
    public void CreatesFlextimeRecordOfDateAndHours()
    {
        var target = new FlextimeViewModel { Date = new DateOnly(2026, 12, 7), Hours = 7.5m };

        var record = target.CreateRecord();

        Assert.Multiple(() =>
        {
            Assert.That(record.IsFlextime, Is.True);
            Assert.That(record.Date, Is.EqualTo(new DateOnly(2026, 12, 7)));
            Assert.That(record.Duration, Is.EqualTo(TimeSpan.FromHours(7.5)));
        });
    }

    [Test]
    public void EmptyHoursCreateZeroDuration()
    {
        var target = new FlextimeViewModel { Hours = null };

        Assert.That(target.CreateRecord().Duration, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void SettingTheSameDateRaisesNothing()
    {
        var target = new FlextimeViewModel();
        var raised = false;
        target.PropertyChanged += (_, _) => raised = true;

        target.Date = target.Date;

        Assert.That(raised, Is.False);
    }

    [Test]
    public void ChangedDateRaisesDateAndHours()
    {
        var day = new DateOnly(2000, 1, 3);
        var target = new FlextimeViewModel(date => date == day ? 6 : 8);
        var changed = new List<string?>();
        target.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        target.Date = day;

        Assert.That(changed, Is.EquivalentTo(new[] { nameof(FlextimeViewModel.Date), nameof(FlextimeViewModel.Hours) }));
    }
}
