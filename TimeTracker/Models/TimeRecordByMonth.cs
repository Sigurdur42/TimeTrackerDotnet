using System;
using System.Diagnostics;
using CsvHelper.Configuration.Attributes;
using ReactiveUI;

namespace TimeTracker.Models;

[DebuggerDisplay("{MonthDisplay} - {Overtime}")]
public partial class TimeRecordByMonth : ReactiveObject
{
    DateOnly _date;
    decimal _overtimeMinutes;


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

    public TimeSpan Overtime => TimeSpan.FromMinutes((double)OvertimeMinutes);

    public string MonthDisplay => $"{Date.Month:d2}.{Date.Year:d4}";
    public string SortDisplay => $"{Date.Year:d4}.{Date.Month:d2}";
}