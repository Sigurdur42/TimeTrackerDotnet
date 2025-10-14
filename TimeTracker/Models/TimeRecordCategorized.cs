using System;
using System.Diagnostics;
using ReactiveUI;

namespace TimeTracker.Models;

[DebuggerDisplay("{Date} - {Duration} - {Category}")]
public class TimeRecordCategorized : ReactiveObject
{
    private string? _category;
    private DateOnly _date;
    private decimal _travelMinutes;
    private decimal _workMinutes;

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