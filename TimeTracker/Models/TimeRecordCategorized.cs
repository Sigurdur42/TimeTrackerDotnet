using System;
using System.Diagnostics;
using ReactiveUI;

namespace TimeTracker.Models;

[DebuggerDisplay("{Date} - {Duration} - {Category}")]
public class TimeRecordCategorized : ReactiveObject
{
    private string? _category;
    private DateOnly _date;
    private decimal _travelActiveMinutes;
    private decimal _travelPassiveMinutes;
    private decimal _workMinutes;

    public DateOnly Date
    {
        get => _date;
        set => this.RaiseAndSetIfChanged(ref _date, value);
    }

    public decimal TravelActiveMinutes
    {
        get => _travelActiveMinutes;
        set => this.RaiseAndSetIfChanged(ref _travelActiveMinutes, value);
    }

    public decimal TravelPassiveMinutes
    {
        get => _travelPassiveMinutes;
        set => this.RaiseAndSetIfChanged(ref _travelPassiveMinutes, value);
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

    public TimeSpan Duration => TimeSpan.FromMinutes((double)(TravelActiveMinutes + TravelPassiveMinutes + WorkMinutes));
}