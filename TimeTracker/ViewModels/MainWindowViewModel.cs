using System;
using System.Collections.ObjectModel;
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
    private string _fileName = string.Empty;
    private ILocalSettings _settings;

    public ObservableCollection<TimeRecord> RawData { get; } = new();

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
    }
}