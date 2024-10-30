using System;
using System.Collections.ObjectModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Config.Net;
using DynamicData;
using ReactiveUI;
using TimeTracker.Controller;
using TimeTracker.Models;

namespace TimeTracker.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ILocalSettings _settings;
    private TimeSpan _totalOvertime;
    private TimeRecord? _selectedDaiy;

    public ObservableCollection<TimeRecord> RawData { get; } = [];
    public ObservableCollection<TimeRecordCategorized> CategorizedData { get; } = [];
    public ObservableCollection<TimeRecordByDay> ByDayData { get; } = [];
    public ObservableCollection<TimeRecordByMonth> ByMonthData { get; } = [];
    public ObservableCollection<TimeRecordExcel> ExcelData { get; } = [];

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

    public MainWindowViewModel()
    {
            var version = GetType()?.Assembly?.GetName()?.Version?.ToString() ?? "";
            var informational = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;
            informational = informational.Split("+")[0];

            ApplicationTitle = $"TimeTracker - V{informational}";


        var special = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var path = Path.Combine(special, "TimeTracker", "TimeTrackerSettings.json");
        _settings = new ConfigurationBuilder<ILocalSettings>()
            .UseJsonFile(path)
            .Build();


        if ((_settings.LastDataFile?.Length ?? 0) > 0)
        {
            LoadDataFile(new FileInfo(_settings.LastDataFile!));
        }
    }

    

    public string? FileName => _settings.LastDataFile;

    public string ApplicationTitle { get; }

    public void LoadDataFile(FileInfo fileInfo)
    {
        if (fileInfo.Exists == false)
        {
            return;
        }

        RawData.Clear();

        var serializer = new TimeRecordSerializer();
        var data = serializer.Deserialize(fileInfo)
            .OrderByDescending(_ => _.Date)
            .ThenByDescending(_ => _.Start)
            .ToArray();

        _settings.LastDataFile = fileInfo.FullName;
        this.RaisePropertyChanged(nameof(FileName));

        RawData.AddRange(data.OrderByDescending(d => d.Date));
        Recalculate();
    }

    public void RecalculateAndSaveData()
    {
        Recalculate();

        var serializer = new TimeRecordSerializer();
        serializer.Serialize(new FileInfo(FileName!), RawData.ToArray());
    }

    private void Recalculate()
    {
        var calculator = new TimeRecordCalculator();
        var raw = RawData.ToArray();

        var categorized = calculator.CalculateCategorySummary(raw);
        CategorizedData.Clear();
        CategorizedData.AddRange(categorized.OrderByDescending(_ => _.Date).ThenBy(_ => _.Category));

        var byDay = calculator.CalculateByDay(raw);
        ByDayData.Clear();
        ByDayData.AddRange(byDay.OrderByDescending(_ => _.Date));

        var byMonth = calculator.CalculateByMonth(byDay);
        ByMonthData.Clear();
        ByMonthData.AddRange(byMonth.OrderByDescending(_ => _.SortDisplay));

        TotalOvertime = TimeSpan.FromMinutes((double)calculator.CalculateTotalOvertime(byMonth));

        var byExcel = calculator.CalculateExcel(byDay);
        ExcelData.Clear();
        ExcelData.AddRange(byExcel.OrderByDescending(_ => _.Date));
    }
}