using System;
using System.Collections.ObjectModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
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

    public ObservableCollection<TimeRecord> RawData { get; } = [];
    public ObservableCollection<TimeRecordCategorized> CategorizedData { get; } = [];
    public ObservableCollection<TimeRecordByDay> ByDayData { get; } = [];
    public ObservableCollection<TimeRecordByMonth> ByMonthData { get; } = [];

    public TimeSpan TotalOvertime
    {
        get => _totalOvertime;
        set => this.RaiseAndSetIfChanged(ref _totalOvertime, value);
    }

    public MainWindowViewModel()
    {
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

    public void LoadDataFile(FileInfo fileInfo)
    {
        if (fileInfo.Exists == false)
        {
            return;
        }

        RawData.Clear();

        var serializer = new TimeRecordSerializer();
        var data = serializer.Deserialize(fileInfo);

        _settings.LastDataFile = fileInfo.FullName;
        this.RaisePropertyChanged(nameof(FileName));

        RawData.AddRange(data.OrderByDescending(d => d.Date));
        Recalculate();
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

        var byMonth = calculator.CalculateByMonth(raw);
        ByMonthData.Clear();
        ByMonthData.AddRange(byMonth.OrderByDescending(_ => _.SortDisplay));

        TotalOvertime = TimeSpan.FromMinutes((double)calculator.CalculateTotalOvertime(byMonth));
    }
}