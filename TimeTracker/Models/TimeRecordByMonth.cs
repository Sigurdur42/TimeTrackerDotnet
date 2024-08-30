using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper.Configuration.Attributes;

namespace TimeTracker.Models;

public partial class TimeRecordByMonth : ObservableObject
{
    [ObservableProperty] DateOnly _date;
    [ObservableProperty] decimal _overtimeMinutes;
    [ObservableProperty] decimal _totalMinutes;

    [Ignore]
    public string MonthDisplay => $"{Date.Month:d2}.{Date.Year:d4}";
}