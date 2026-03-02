using System;
using System.Diagnostics;
using ReactiveUI;

namespace TimeTracker.Models;

[DebuggerDisplay("{MonthDisplay} - {Overtime}")]
public class TimeRecordByMonth : ReactiveObject
{
    private DateOnly _date;
    private decimal _overtimeMinutes;
    private TimeSpan _overtimeTotal;

    public DateOnly Date
    {
        get => _date;
        set => this.RaiseAndSetIfChanged(ref _date, value);
    }

    public decimal OvertimeMinutes
    {
        get => _overtimeMinutes;
        set => this.RaiseAndSetIfChanged(ref _overtimeMinutes, value);
    }

    public TimeSpan OvertimeTotal
    {
        get => _overtimeTotal;
        set => this.RaiseAndSetIfChanged(ref _overtimeTotal, value);
    }

    public TimeSpan Overtime => TimeSpan.FromMinutes((double)OvertimeMinutes);

    public string MonthDisplay => $"{Date.Month:d2}.{Date.Year:d4}";
    public string SortDisplay => $"{Date.Year:d4}.{Date.Month:d2}";
}