using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TimeTracker.Models;

public partial class TimeRecordByDay : ObservableObject
{
    [ObservableProperty] DateOnly _date;
    [ObservableProperty] decimal _overtimeMinutes;
}