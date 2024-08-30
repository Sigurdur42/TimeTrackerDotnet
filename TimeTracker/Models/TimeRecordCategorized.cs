using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TimeTracker.Models;

public partial class TimeRecordCategorized : ObservableObject
{
    [ObservableProperty] DateOnly _date;
    [ObservableProperty] decimal _workMinutes;
    [ObservableProperty] decimal _travelMinutes;
    [ObservableProperty] string? _category;
}