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
        Recalculate(data, false);
    }

    public void RecalculateAndSaveData()
    {
        var raw = RawData.ToArray();
        Recalculate(raw, true);

        _serializer.Serialize(new FileInfo(FileName!), raw);
    }

    // Display order of the derived collections; dates are unique per day/month
    private static readonly Comparison<TimeRecordCategorized> CategorizedOrder = (a, b) =>
    {
        var result = b.Date.CompareTo(a.Date);
        return result != 0 ? result : string.Compare(a.Category, b.Category, StringComparison.CurrentCulture);
    };

    private static readonly Comparison<TimeRecordByDay> ByDayOrder = (a, b) => b.Date.CompareTo(a.Date);
    private static readonly Comparison<TimeRecordByMonth> ByMonthOrder = (a, b) => b.Date.CompareTo(a.Date);
    private static readonly Comparison<TimeRecordExcel> ExcelOrder = (a, b) => b.Date.CompareTo(a.Date);

    /// <param name="raw">All time records</param>
    /// <param name="incremental">
    ///     True to merge the results into the existing collections so that only the affected grid rows are re-rendered,
    ///     false to replace the whole content (e.g. when a different file is loaded)
    /// </param>
    private void Recalculate(TimeRecord[] raw, bool incremental)
    {
        var categorized = _calculator.CalculateCategorySummary(raw);
        Array.Sort(categorized, CategorizedOrder);
        Apply(CategorizedData, categorized, CategorizedOrder, (a, b) =>
            a.Category == b.Category && a.WorkMinutes == b.WorkMinutes && a.TravelMinutes == b.TravelMinutes);

        // Ascending order is needed for the running totals, the display order is the reverse
        var byDay = _calculator.CalculateByDay(raw);
        Array.Sort(byDay, (a, b) => a.Date.CompareTo(b.Date));

        _calculator.CalculateWeeklyTotals(byDay);

        var byMonth = _calculator.CalculateByMonth(byDay);
        Array.Sort(byMonth, (a, b) => a.Date.CompareTo(b.Date));

        _calculator.CalculateRunningTotals(byMonth);

        TotalOvertime = TimeSpan.FromMinutes((double)_calculator.CalculateTotalOvertime(byMonth));

        var excel = _calculator.CalculateExcel(byDay);

        Array.Reverse(byDay);
        Apply(ByDayData, byDay, ByDayOrder, (a, b) =>
            a.OvertimeMinutes == b.OvertimeMinutes && a.OvertimeThisWeek == b.OvertimeThisWeek && a.IsLastOfWeek == b.IsLastOfWeek &&
            a.WorkMinutes == b.WorkMinutes && a.TravelMinutes == b.TravelMinutes && a.Start == b.Start);

        Array.Reverse(byMonth);
        Apply(ByMonthData, byMonth, ByMonthOrder, (a, b) =>
            a.OvertimeMinutes == b.OvertimeMinutes && a.OvertimeTotal == b.OvertimeTotal);

        Array.Reverse(excel);
        Apply(ExcelData, excel, ExcelOrder, (a, b) =>
            a.Start == b.Start && a.End == b.End && a.TravelStart == b.TravelStart && a.TravelEnd == b.TravelEnd);

        return;

        void Apply<T>(RangeObservableCollection<T> target, T[] items, Comparison<T> order, Func<T, T, bool> sameValues)
        {
            if (incremental) target.Update(items, order, sameValues);
            else target.Reset(items);
        }
    }
}
