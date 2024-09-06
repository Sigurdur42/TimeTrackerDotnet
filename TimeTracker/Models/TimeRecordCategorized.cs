using System;
using ReactiveUI;

namespace TimeTracker.Models;

public partial class TimeRecordCategorized : ReactiveObject
{
    DateOnly _date;
    decimal _workMinutes;
    decimal _travelMinutes;
    string? _category;

    public DateOnly Date
    {
        get => _date;
        set => this.RaiseAndSetIfChanged(ref _date, value);
    }

    public decimal TravelMinutes
    {
        get => _travelMinutes;
        set => this.RaiseAndSetIfChanged(ref _travelMinutes, value);
    }

    public decimal WorkMinutes
    {
        get => _workMinutes;
        set => this.RaiseAndSetIfChanged(ref _workMinutes, value);
    }

    public string? Category
    {
        get => _category;
        set => this.RaiseAndSetIfChanged(ref _category, value);
    }
    
    public TimeSpan Duration => TimeSpan.FromMinutes((double)(TravelMinutes + WorkMinutes));
}