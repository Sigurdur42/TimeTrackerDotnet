using System.Globalization;
using TimeTracker.Controller;
using TimeTracker.Models;
using TimeTracker.ViewModels;

namespace TimeTracker.Tests;

public class MainViewModelTests
{
    private string _tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Directory.CreateTempSubdirectory("TimeTrackerTests").FullName;
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_tempDirectory, true);
    }

    [TestCase(null)]
    [TestCase("")]
    public void CreatesDefaultFileIfNoLastFile(string? lastDataFile)
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");

        var result = MainWindowViewModel.GetOrCreateDataFile(lastDataFile, appDirectory);

        Assert.Multiple(() =>
        {
            Assert.That(result.FullName, Is.EqualTo(Path.Combine(appDirectory, MainWindowViewModel.DefaultDataFileName)));
            Assert.That(result.Exists, Is.True);
            Assert.That(result.Length, Is.Zero);
            Assert.That(new TimeRecordSerializer().Deserialize(result), Is.Empty);
        });
    }

    [Test]
    public void KeepsExistingLastFile()
    {
        var lastDataFile = Path.Combine(_tempDirectory, "data.csv");
        File.WriteAllText(lastDataFile, "03.09.2026;08:30;13:00;;Entry;\n");

        var result = MainWindowViewModel.GetOrCreateDataFile(lastDataFile, Path.Combine(_tempDirectory, "app"));

        Assert.Multiple(() =>
        {
            Assert.That(result.FullName, Is.EqualTo(lastDataFile));
            Assert.That(new TimeRecordSerializer().Deserialize(result), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void KeepsExistingDefaultFile()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        var defaultFile = Path.Combine(appDirectory, MainWindowViewModel.DefaultDataFileName);
        File.WriteAllText(defaultFile, "03.09.2026;08:30;13:00;;Entry;\n");

        var result = MainWindowViewModel.GetOrCreateDataFile(null, appDirectory);

        Assert.That(new TimeRecordSerializer().Deserialize(result), Has.Length.EqualTo(1));
    }

    [Test]
    public void RecreatesMissingLastFileIfDirectoryExists()
    {
        var lastDataFile = Path.Combine(_tempDirectory, "data.csv");

        var result = MainWindowViewModel.GetOrCreateDataFile(lastDataFile, Path.Combine(_tempDirectory, "app"));

        Assert.Multiple(() =>
        {
            Assert.That(result.FullName, Is.EqualTo(lastDataFile));
            Assert.That(result.Exists, Is.True);
        });
    }

    private sealed class TestSettings : ILocalSettings
    {
        public string? LastDataFile { get; set; }
        public double DailyTargetHours { get; set; } = 8;
    }

    private static TimeRecord Record(string date, string start, string end)
    {
        return new TimeRecord
        {
            Date = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Start = TimeOnly.ParseExact(start, "HH:mm", CultureInfo.InvariantCulture),
            End = TimeOnly.ParseExact(end, "HH:mm", CultureInfo.InvariantCulture)
        };
    }

    private MainWindowViewModel CreateViewModel(string fileContent, double dailyTargetHours = 8)
    {
        var dataFile = Path.Combine(_tempDirectory, "data.csv");
        File.WriteAllText(dataFile, fileContent);
        return new MainWindowViewModel(Path.Combine(_tempDirectory, "app"), new TestSettings { LastDataFile = dataFile, DailyTargetHours = dailyTargetHours });
    }

    [Test]
    public void InvalidLineDoesNotPreventStartAndIsShown()
    {
        var target = CreateViewModel("05.02.2024;;10:00;;a;\n");

        Assert.Multiple(() =>
        {
            Assert.That(target.RawData, Is.Empty);
            Assert.That(target.ErrorMessage, Does.Contain("data.csv").And.Contain("record 1"));
        });
    }

    [Test]
    public void FileThatCouldNotBeLoadedIsNotOverwritten()
    {
        const string content = "05.02.2024;;10:00;;a;\n06.02.2024;08:00;10:00;;b;\n";
        var target = CreateViewModel(content);

        target.AddRecord(Record("2024-02-07", "08:00", "09:00"));
        var saved = target.RecalculateAndSaveData();

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.False);
            Assert.That(File.ReadAllText(Path.Combine(_tempDirectory, "data.csv")), Is.EqualTo(content));
            Assert.That(target.ErrorMessage, Does.Contain("not saved"));
        });
    }

    [Test]
    public void LoadingAnInvalidFileKeepsTheCurrentData()
    {
        var target = CreateViewModel("05.02.2024;08:00;10:00;;a;\n");
        var invalidFile = Path.Combine(_tempDirectory, "invalid.csv");
        File.WriteAllText(invalidFile, "05.02.2024;08:00;;;a;\n");

        var loaded = target.LoadDataFile(new FileInfo(invalidFile));
        target.AddRecord(Record("2024-02-06", "08:00", "09:00"));
        var saved = target.RecalculateAndSaveData();

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.False);
            Assert.That(target.FileName, Does.EndWith("data.csv"));
            Assert.That(saved, Is.True, "Saved into the file loaded before");
            Assert.That(File.ReadAllText(Path.Combine(_tempDirectory, "data.csv")), Does.Contain("06.02.2024"));
            Assert.That(File.ReadAllText(invalidFile), Is.EqualTo("05.02.2024;08:00;;;a;\n"));
            Assert.That(target.ErrorMessage, Is.Null, "Cleared by the successful save");
        });
    }

    [Test]
    public void InvalidTargetHoursAreShownAndStayVisible()
    {
        var target = CreateViewModel("05.02.2024;08:00;10:00;;a;\n", 0);

        target.ReportError("Deleting the record", new IOException("locked"));
        var withActionError = target.ErrorMessage;
        target.RecalculateAndSaveData();

        Assert.Multiple(() =>
        {
            Assert.That(withActionError, Does.Contain("DailyTargetHours").And.Contain("Deleting the record failed: locked"));
            Assert.That(target.ErrorMessage, Does.Contain("DailyTargetHours").And.Not.Contain("locked"), "Successful save clears the action error only");
            Assert.That(target.GetDefaultFlextimeHours(new DateOnly(2026, 12, 7)), Is.EqualTo(8), "Default hours are used");
        });
    }

    [Test]
    public void ReportedErrorIsShown()
    {
        var target = CreateViewModel("05.02.2024;08:00;10:00;;a;\n");

        target.ReportError("Adding the flextime", new InvalidOperationException("boom"));

        Assert.That(target.ErrorMessage, Is.EqualTo("Adding the flextime failed: boom"));
    }

    [Test]
    public void ValidFileHasNoError()
    {
        var target = CreateViewModel("05.02.2024;08:00;10:00;;a;\n");

        Assert.Multiple(() =>
        {
            Assert.That(target.RawData, Has.Count.EqualTo(1));
            Assert.That(target.ErrorMessage, Is.Null);
        });
    }

    [Test]
    public void NewRecordIsInsertedByDateAndStart()
    {
        var target = CreateViewModel("06.02.2024;08:00;10:00;;a;\n05.02.2024;08:00;10:00;;b;\n05.02.2024;14:00;15:00;;c;\n");

        target.AddRecord(Record("2024-02-05", "11:00", "12:00"));

        Assert.That(target.RawData.Select(_ => $"{_.Date:dd.MM} {_.Start:HH:mm}"),
            Is.EqualTo(new[] { "06.02 08:00", "05.02 14:00", "05.02 11:00", "05.02 08:00" }));
    }

    [Test]
    public void EditedRecordIsMovedToItsPositionAndStaysSelected()
    {
        var target = CreateViewModel("07.02.2024;08:00;10:00;;a;\n06.02.2024;08:00;10:00;;b;\n05.02.2024;08:00;10:00;;c;\n");
        var record = target.RawData[0];
        target.SelectedRecord = record;

        record.Date = new DateOnly(2024, 2, 5);
        record.Start = new TimeOnly(9, 0);
        target.UpdateRecordPosition(record);

        Assert.Multiple(() =>
        {
            Assert.That(target.RawData.Select(_ => _.Comment), Is.EqualTo(new[] { "b", "a", "c" }));
            Assert.That(target.SelectedRecord, Is.SameAs(record));
        });
    }

    [Test]
    public void EditedRecordAtItsPositionIsNotMoved()
    {
        var target = CreateViewModel("07.02.2024;08:00;10:00;;a;\n06.02.2024;08:00;10:00;;b;\n");
        var record = target.RawData[1];
        var changes = 0;
        target.RawData.CollectionChanged += (_, _) => changes++;

        record.Start = new TimeOnly(9, 0);
        target.UpdateRecordPosition(record);

        Assert.That(changes, Is.Zero);
    }

    [TestCase("2026-11-30", 8, Description = "Old rules always use 8 hours")]
    [TestCase("2026-12-01", 7.5, Description = "New rules use the configured hours")]
    public void FlextimeDefaultsToTargetHoursOfTheDay(string date, double expectedHours)
    {
        var target = CreateViewModel("", 7.5);

        var result = target.GetDefaultFlextimeHours(DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture));

        Assert.That(result, Is.EqualTo((decimal)expectedHours));
    }

    [TestCase("2026-11-30", 4, Description = "Old rules always use 8 hours")]
    [TestCase("2026-12-01", 3.75, Description = "New rules use the configured hours")]
    public void HalfSickDayIsHalfOfTheTargetHoursOfTheDay(string date, double expectedHours)
    {
        var target = CreateViewModel("", 7.5);

        var result = target.GetHalfSickDayHours(DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture));

        Assert.That(result, Is.EqualTo((decimal)expectedHours));
    }

    [Test]
    public void HalfSickDayIsSavedAndCredited()
    {
        // Monday: 5 h work + half of 8 h sick => 1 h overtime
        var target = CreateViewModel("05.02.2024;08:00;13:00;;a;\n");

        target.AddRecord(TimeRecord.CreateSick(new DateOnly(2024, 2, 5), target.GetHalfSickDayHours(new DateOnly(2024, 2, 5))));
        target.RecalculateAndSaveData();

        Assert.Multiple(() =>
        {
            Assert.That(target.TotalOvertime, Is.EqualTo(TimeSpan.FromHours(1)));
            Assert.That(File.ReadAllText(Path.Combine(_tempDirectory, "data.csv")), Does.Contain("05.02.2024;00:00;04:00;;Krank;;"));
        });
    }

    [Test]
    public void FlextimeHoursFollowTheDateUntilChanged()
    {
        var target = new FlextimeViewModel(date => date < new DateOnly(2026, 12, 1) ? 8 : 7);

        target.Date = new DateOnly(2026, 12, 7);
        var followed = target.Hours;

        target.Hours = 4;
        target.Date = new DateOnly(2026, 11, 30);

        Assert.Multiple(() =>
        {
            Assert.That(followed, Is.EqualTo(7));
            Assert.That(target.Hours, Is.EqualTo(4), "Hours changed by the user are kept");
        });
    }

    [Test]
    public void DeleteRemovesExactlyTheInstanceAndClearsSelection()
    {
        var target = CreateViewModel("05.02.2024;08:00;10:00;;a;\n05.02.2024;08:00;10:00;;a;\n");
        var deleted = target.RawData[1];
        var kept = target.RawData[0];
        target.SelectedRecord = deleted;

        target.DeleteRecord(deleted);

        Assert.Multiple(() =>
        {
            Assert.That(target.RawData, Has.Count.EqualTo(1));
            Assert.That(target.RawData[0], Is.SameAs(kept));
            Assert.That(target.SelectedRecord, Is.Null);
        });
    }

    [Test]
    public void DeleteKeepsSelectionOfOtherRecord()
    {
        var target = CreateViewModel("06.02.2024;08:00;10:00;;a;\n05.02.2024;08:00;10:00;;b;\n");
        var selected = target.RawData[0];
        target.SelectedRecord = selected;

        target.DeleteRecord(target.RawData[1]);

        Assert.That(target.SelectedRecord, Is.SameAs(selected));
    }

    [Test]
    public void DeletingAnUnknownRecordChangesNothing()
    {
        var target = CreateViewModel("05.02.2024;08:00;10:00;;a;\n");
        var equalButOther = new TimeRecord();
        target.RawData[0].CopyTo(equalButOther);

        target.DeleteRecord(equalButOther);

        Assert.That(target.RawData, Has.Count.EqualTo(1));
    }

    [Test]
    public void DeletedRecordIsRemovedFromTheFile()
    {
        var target = CreateViewModel("06.02.2024;08:00;10:00;;a;\n05.02.2024;08:00;10:00;;b;\n");

        target.DeleteRecord(target.RawData[0]);
        target.RecalculateAndSaveData();

        Assert.That(new TimeRecordSerializer().Deserialize(new FileInfo(Path.Combine(_tempDirectory, "data.csv"))).Select(_ => _.Comment),
            Is.EqualTo(new[] { "b" }));
    }

    [Test]
    public void LoadingAMissingFileKeepsTheCurrentData()
    {
        var target = CreateViewModel("05.02.2024;08:00;10:00;;a;\n");

        var loaded = target.LoadDataFile(new FileInfo(Path.Combine(_tempDirectory, "missing.csv")));

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.False);
            Assert.That(target.RawData, Has.Count.EqualTo(1));
            Assert.That(target.FileName, Does.EndWith("data.csv"));
            Assert.That(target.ErrorMessage, Does.Contain("missing.csv").And.Contain("does not exist"));
        });
    }

    [Test]
    public void LoadingAnotherFileReplacesTheData()
    {
        var target = CreateViewModel("05.02.2024;08:00;10:00;;a;\n");
        var otherFile = Path.Combine(_tempDirectory, "other.csv");
        File.WriteAllText(otherFile, "06.02.2024;08:00;09:00;;x;\n07.02.2024;08:00;09:00;;y;\n");

        var loaded = target.LoadDataFile(new FileInfo(otherFile));

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.True);
            Assert.That(target.FileName, Is.EqualTo(otherFile));
            Assert.That(target.RawData.Select(_ => _.Comment), Is.EqualTo(new[] { "y", "x" }), "Newest first");
            Assert.That(target.ErrorMessage, Is.Null);
        });
    }

    [Test]
    public void LockedFileIsNotSavedAndShown()
    {
        var target = CreateViewModel("05.02.2024;08:00;10:00;;a;\n");
        bool saved;

        using (File.Open(Path.Combine(_tempDirectory, "data.csv"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            saved = target.RecalculateAndSaveData();
        }

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.False);
            Assert.That(target.ErrorMessage, Does.StartWith("Could not save").And.Contain("data.csv"));
        });
    }

    [Test]
    public void DataFileThatCannotBeCreatedIsShown()
    {
        // A file where the app directory should be prevents creating the default data file
        var appDirectory = Path.Combine(_tempDirectory, "app");
        File.WriteAllText(appDirectory, "");

        var target = new MainWindowViewModel(appDirectory, new TestSettings());

        Assert.Multiple(() =>
        {
            Assert.That(target.RawData, Is.Empty);
            Assert.That(target.ErrorMessage, Does.StartWith("Could not open the data file"));
            Assert.That(target.RecalculateAndSaveData(), Is.False, "Nothing to save into");
        });
    }

    [Test]
    public void TotalOvertimeIsCalculatedOnLoadAndSave()
    {
        // Monday, 10 hours => 2 hours overtime
        var target = CreateViewModel("05.02.2024;08:00;18:00;;a;\n");
        var afterLoad = target.TotalOvertime;

        target.AddRecord(Record("2024-02-06", "08:00", "17:00"));
        target.RecalculateAndSaveData();

        Assert.Multiple(() =>
        {
            Assert.That(afterLoad, Is.EqualTo(TimeSpan.FromHours(2)));
            Assert.That(target.TotalOvertime, Is.EqualTo(TimeSpan.FromHours(3)));
            Assert.That(target.ByDayData, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void TitleContainsTheVersion()
    {
        var target = CreateViewModel("");

        Assert.That(target.ApplicationTitle, Does.StartWith("TimeTracker - V"));
    }

    [Test]
    public void FallsBackToDefaultFileIfLastDirectoryIsMissing()
    {
        var lastDataFile = Path.Combine(_tempDirectory, "unmounted", "data.csv");
        var appDirectory = Path.Combine(_tempDirectory, "app");

        var result = MainWindowViewModel.GetOrCreateDataFile(lastDataFile, appDirectory);

        Assert.Multiple(() =>
        {
            Assert.That(result.FullName, Is.EqualTo(Path.Combine(appDirectory, MainWindowViewModel.DefaultDataFileName)));
            Assert.That(result.Exists, Is.True);
            Assert.That(Directory.Exists(Path.GetDirectoryName(lastDataFile)), Is.False);
        });
    }
}
