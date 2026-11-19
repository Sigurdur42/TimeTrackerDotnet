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

    private readonly TimeRecordCalculator _calculator;
    private readonly TimeRecordSerializer _serializer = new();
    private readonly ILocalSettings _settings;
    private FileInfo? _dataFile;
    private string? _errorMessage;
    private TimeRecord? _selectedDaiy;
    private TimeSpan _totalOvertime;

    public MainWindowViewModel() : this(DefaultAppDirectory, null)
    {
    }

    /// <param name="appDirectory">Directory of the settings and the default data file</param>
    /// <param name="settings">The settings to use, null to read them from the app directory</param>
    public MainWindowViewModel(string appDirectory, ILocalSettings? settings)
    {
        var version = GetType().Assembly.GetName().Version?.ToString() ?? "";
        var informational = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;
        informational = informational.Split('+')[0];

        ApplicationTitle = $"TimeTracker - V{informational}";

        _settings = settings ?? new ConfigurationBuilder<ILocalSettings>().UseJsonFile(Path.Combine(appDirectory, "TimeTrackerSettings.json")).Build();
        _calculator = CreateCalculator(_settings.DailyTargetHours);

        FileInfo file;
        try
        {
            file = GetOrCreateDataFile(_settings.LastDataFile, appDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = $"Could not open the data file: {exception.Message}";
            return;
        }

        LoadDataFile(file);
    }

    private static string DefaultAppDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TimeTracker");

    /// <summary>
    ///     Error of loading or saving the data file, null if there is none
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    /// <summary>
    ///     Default hours of a flextime record on the given day, the daily target hours of that day
    /// </summary>
    public decimal GetDefaultFlextimeHours(DateOnly date)
    {
        return _calculator.GetDailyTargetMinutes(date) / 60;
    }

    private static TimeRecordCalculator CreateCalculator(double dailyTargetHours)
    {
        try
        {
            return new TimeRecordCalculator(dailyTargetHours);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            // An invalid setting must not prevent the application from starting
            Console.WriteLine(exception);
            return new TimeRecordCalculator();
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

    public string? FileName => _dataFile?.FullName ?? _settings.LastDataFile;

    public string ApplicationTitle { get; }

    /// <summary>
    ///     Loads the data file. If it cannot be read, the current data is kept and <see cref="ErrorMessage" /> is set.
    /// </summary>
    /// <returns>True if the file was loaded</returns>
    public bool LoadDataFile(FileInfo fileInfo)
    {
        if (!fileInfo.Exists)
        {
            ErrorMessage = $"The data file {fileInfo.FullName} does not exist";
            return false;
        }

        TimeRecord[] data;
        try
        {
            data = _serializer.Deserialize(fileInfo).OrderByDescending(_ => _.Date).ThenByDescending(_ => _.Start).ToArray();
        }
        catch (Exception exception)
        {
            // Any error of a single line must not prevent the application from starting
            ErrorMessage = $"Could not load {fileInfo.FullName}: {exception.Message}";
            return false;
        }

        _dataFile = fileInfo;
        _settings.LastDataFile = fileInfo.FullName;
        ErrorMessage = null;
        this.RaisePropertyChanged(nameof(FileName));

        RawData.Reset(data);
        Recalculate(data, false);
        return true;
    }

    /// <summary>
    ///     Inserts the record at its position in <see cref="RawData" />, which is sorted descending by date and start.
    /// </summary>
    public void AddRecord(TimeRecord record)
    {
        var index = 0;
        while (index < RawData.Count && (RawData[index].Date > record.Date || (RawData[index].Date == record.Date && RawData[index].Start > record.Start))) index++;

        RawData.Insert(index, record);
    }

    /// <summary>
    ///     Moves the record to its position after its date or start was changed. The selection is kept.
    /// </summary>
    public void UpdateRecordPosition(TimeRecord record)
    {
        var index = IndexOf(record);
        if (index < 0) return;

        var isBeforeNext = index + 1 >= RawData.Count || Compare(record, RawData[index + 1]) <= 0;
        var isAfterPrevious = index == 0 || Compare(RawData[index - 1], record) <= 0;
        if (isBeforeNext && isAfterPrevious) return;

        var wasSelected = ReferenceEquals(SelectedDaiy, record);
        RawData.RemoveAt(index);
        AddRecord(record);
        if (wasSelected) SelectedDaiy = record;

        return;

        // Order of RawData: descending by date and start
        static int Compare(TimeRecord a, TimeRecord b)
        {
            var result = b.Date.CompareTo(a.Date);
            return result != 0 ? result : b.Start.CompareTo(a.Start);
        }
    }

    private int IndexOf(TimeRecord record)
    {
        for (var index = 0; index < RawData.Count; index++)
            if (ReferenceEquals(RawData[index], record))
                return index;

        return -1;
    }

    /// <summary>
    ///     Removes exactly the given record instance; records with equal values are kept.
    /// </summary>
    public void DeleteRecord(TimeRecord record)
    {
        var index = IndexOf(record);
        if (index < 0) return;

        if (ReferenceEquals(SelectedDaiy, record)) SelectedDaiy = null;
        RawData.RemoveAt(index);
    }

    /// <summary>
    ///     Recalculates and saves the data. Without a successfully loaded data file nothing is saved, as this would
    ///     overwrite the file that could not be read.
    /// </summary>
    /// <returns>True if the data was saved</returns>
    public bool RecalculateAndSaveData()
    {
        var raw = RawData.ToArray();
        Recalculate(raw, true);

        if (_dataFile is null)
        {
            ErrorMessage = "Changes are not saved, as no data file is loaded. Fix the file and load it again.";
            return false;
        }

        try
        {
            _serializer.Serialize(_dataFile, raw);
            ErrorMessage = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ErrorMessage = $"Could not save {_dataFile.FullName}: {exception.Message}";
            return false;
        }
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
            a.Category == b.Category && a.WorkMinutes == b.WorkMinutes && a.TravelActiveMinutes == b.TravelActiveMinutes && a.TravelPassiveMinutes == b.TravelPassiveMinutes);

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
            a.WorkMinutes == b.WorkMinutes && a.TravelActiveMinutes == b.TravelActiveMinutes && a.TravelPassiveMinutes == b.TravelPassiveMinutes && a.Start == b.Start &&
            a.WorkCapLossMinutes == b.WorkCapLossMinutes && a.PassiveTravelLossMinutes == b.PassiveTravelLossMinutes && a.SurchargeMinutes == b.SurchargeMinutes);

        Array.Reverse(byMonth);
        Apply(ByMonthData, byMonth, ByMonthOrder, (a, b) =>
            a.OvertimeMinutes == b.OvertimeMinutes && a.OvertimeTotal == b.OvertimeTotal);

        Array.Reverse(excel);
        Apply(ExcelData, excel, ExcelOrder, (a, b) =>
            a.Start == b.Start && a.End == b.End && a.TravelActiveStart == b.TravelActiveStart && a.TravelActiveEnd == b.TravelActiveEnd &&
            a.TravelPassiveStart == b.TravelPassiveStart && a.TravelPassiveEnd == b.TravelPassiveEnd);

        return;

        void Apply<T>(RangeObservableCollection<T> target, T[] items, Comparison<T> order, Func<T, T, bool> sameValues)
        {
            if (incremental) target.Update(items, order, sameValues);
            else target.Reset(items);
        }
    }
}
