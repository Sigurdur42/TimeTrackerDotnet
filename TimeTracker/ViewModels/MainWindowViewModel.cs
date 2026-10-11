using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Config.Net;
using ReactiveUI;
using TimeTracker.Controller;
using TimeTracker.Models;

namespace TimeTracker.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public const string DefaultDataFileName = "TimeTracker.csv";

    private readonly TimeRecordCalculator _calculator = new();
    private readonly TimeRecordSerializer _serializer = new();
    private readonly ILocalSettings _settings;
    private TimeRecord? _selectedDaiy;
    private TimeSpan _totalOvertime;

    public MainWindowViewModel()
    {
        var version = GetType().Assembly.GetName().Version?.ToString() ?? "";
        var informational = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;
        informational = informational.Split('+')[0];

        ApplicationTitle = $"TimeTracker - V{informational}";

        var special = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDirectory = Path.Combine(special, "TimeTracker");
        _settings = new ConfigurationBuilder<ILocalSettings>().UseJsonFile(Path.Combine(appDirectory, "TimeTrackerSettings.json")).Build();

        try
        {
            LoadDataFile(GetOrCreateDataFile(_settings.LastDataFile, appDirectory));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // TODO: Better error handling
            Console.WriteLine(exception);
        }
    }

    /// <summary>
    ///     Returns the last used data file. If it does not exist, an empty one is created - at the last used location
    ///     if its directory still exists (e.g. drive is mounted), otherwise as default file in the app directory.
    /// </summary>
    public static FileInfo GetOrCreateDataFile(string? lastDataFile, string appDirectory)
    {
        var useLastFile = !string.IsNullOrEmpty(lastDataFile) && Directory.Exists(Path.GetDirectoryName(lastDataFile));
        var file = new FileInfo(useLastFile ? lastDataFile! : Path.Combine(appDirectory, DefaultDataFileName));

        if (!file.Exists)
        {
            file.Directory?.Create();
            File.Create(file.FullName).Dispose();
            file.Refresh();
        }

        return file;
    }

    public RangeObservableCollection<TimeRecord> RawData { get; } = [];
    public RangeObservableCollection<TimeRecordCategorized> CategorizedData { get; } = [];
    public RangeObservableCollection<TimeRecordByDay> ByDayData { get; } = [];
    public RangeObservableCollection<TimeRecordByMonth> ByMonthData { get; } = [];
    public RangeObservableCollection<TimeRecordExcel> ExcelData { get; } = [];

    public TimeSpan TotalOvertime
    {
        get => _totalOvertime;
        set => this.RaiseAndSetIfChanged(ref _totalOvertime, value);
    }

    public TimeRecord? SelectedDaiy
    {
        get => _selectedDaiy;
        set => this.RaiseAndSetIfChanged(ref _selectedDaiy, value);
    }

    public string? FileName => _settings.LastDataFile;

    public string ApplicationTitle { get; }

    public void LoadDataFile(FileInfo fileInfo)
    {
        if (!fileInfo.Exists) return;

        var data = _serializer.Deserialize(fileInfo).OrderByDescending(_ => _.Date).ThenByDescending(_ => _.Start).ToArray();

        _settings.LastDataFile = fileInfo.FullName;
        this.RaisePropertyChanged(nameof(FileName));

        RawData.Reset(data);
        Recalculate(data);
    }

    public void RecalculateAndSaveData()
    {
        var raw = RawData.ToArray();
        Recalculate(raw);

        _serializer.Serialize(new FileInfo(FileName!), raw);
    }

    private void Recalculate(TimeRecord[] raw)
    {
        var categorized = _calculator.CalculateCategorySummary(raw);
        CategorizedData.Reset(categorized.OrderByDescending(_ => _.Date).ThenBy(_ => _.Category));

        // Dates are unique per day/month, so ascending order is computed once and reversed for display
        var byDay = _calculator.CalculateByDay(raw);
        Array.Sort(byDay, (a, b) => a.Date.CompareTo(b.Date));

        var total = TimeSpan.Zero;
        TimeRecordByDay? lastDay = null;
        foreach (var item in byDay)
        {
            if (item.Date.DayOfWeek == DayOfWeek.Monday)
            {
                if (lastDay != null) lastDay.IsLastOfWeek = true;
                total = TimeSpan.Zero;
            }

            total += item.Overtime;
            item.OvertimeThisWeek = total;

            lastDay = item;
        }

        if (lastDay != null) lastDay.IsLastOfWeek = true;

        ByDayData.Reset(Enumerable.Reverse(byDay));

        var byMonth = _calculator.CalculateByMonth(byDay);
        Array.Sort(byMonth, (a, b) => a.Date.CompareTo(b.Date));

        total = TimeSpan.Zero;
        foreach (var item in byMonth)
        {
            total += item.Overtime;
            item.OvertimeTotal = total;
        }

        ByMonthData.Reset(Enumerable.Reverse(byMonth));

        TotalOvertime = TimeSpan.FromMinutes((double)_calculator.CalculateTotalOvertime(byMonth));

        ExcelData.Reset(Enumerable.Reverse(_calculator.CalculateExcel(byDay)));
    }
}
