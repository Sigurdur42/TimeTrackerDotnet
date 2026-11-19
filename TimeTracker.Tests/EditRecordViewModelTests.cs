using TimeTracker.Models;
using TimeTracker.ViewModels;

namespace TimeTracker.Tests;

public class EditRecordViewModelTests
{
    private static EditRecordViewModel Create(string start, string end)
    {
        var record = new TimeRecord
        {
            Date = new DateOnly(2026, 12, 7),
            Start = TimeOnly.Parse(start),
            End = TimeOnly.Parse(end)
        };
        return new EditRecordViewModel(record, true);
    }

    [Test]
    public void PlausibleTimesAreSavedWithoutConfirmation()
    {
        var target = Create("08:00", "17:00");

        Assert.Multiple(() =>
        {
            Assert.That(target.Warning, Is.Null);
            Assert.That(target.Confirm(), Is.True);
        });
    }

    [TestCase("17:00", "08:00", "over midnight")]
    [TestCase("08:00", "08:00", "duration is 0")]
    public void ImplausibleTimesNeedConfirmation(string start, string end, string expectedWarning)
    {
        var target = Create(start, end);

        var first = target.Confirm();
        var pending = target.IsConfirmationPending;
        var second = target.Confirm();

        Assert.Multiple(() =>
        {
            Assert.That(target.Warning, Does.Contain(expectedWarning));
            Assert.That(first, Is.False, "First OK only shows the warning");
            Assert.That(pending, Is.True);
            Assert.That(second, Is.True, "Second OK saves");
        });
    }

    [Test]
    public void WarningShowsDurationOverMidnight()
    {
        var target = Create("17:00", "08:00");

        Assert.That(target.Warning, Does.Contain("15:00 h"));
    }

    [Test]
    public void ChangedTimeNeedsNewConfirmation()
    {
        var target = Create("17:00", "08:00");
        target.Confirm();

        target.TimeRecord.End = new TimeOnly(7, 0);

        Assert.Multiple(() =>
        {
            Assert.That(target.IsConfirmationPending, Is.False);
            Assert.That(target.Confirm(), Is.False);
        });
    }

    [Test]
    public void FixedTimeRemovesWarning()
    {
        var target = Create("17:00", "08:00");
        var changes = new List<string?>();
        target.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

        target.TimeRecord.End = new TimeOnly(18, 0);

        Assert.Multiple(() =>
        {
            Assert.That(target.Warning, Is.Null);
            Assert.That(changes, Does.Contain(nameof(EditRecordViewModel.Warning)));
            Assert.That(target.Confirm(), Is.True);
        });
    }

    [Test]
    public void OriginalRecordIsNotChanged()
    {
        var record = new TimeRecord { Start = new TimeOnly(8, 0), End = new TimeOnly(9, 0) };
        var target = new EditRecordViewModel(record, true);

        target.TimeRecord.End = new TimeOnly(10, 0);

        Assert.That(record.End, Is.EqualTo(new TimeOnly(9, 0)));
    }
}
