using TimeTracker.Controller;
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
