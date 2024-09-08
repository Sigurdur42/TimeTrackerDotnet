using System;
using CsvHelper.Configuration.Attributes;
using ReactiveUI;

namespace TimeTracker.Models;

public partial class TimeRecordByMonth : ReactiveObject
{
    DateOnly _date;
    decimal _overtimeMinutes;
    decimal _totalMinutes;

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

    public decimal TotalMinutes
    {
        get => _totalMinutes;
        set => this.RaiseAndSetIfChanged(ref _totalMinutes, value);
    }

    public TimeSpan Overtime => TimeSpan.FromMinutes((double)OvertimeMinutes);

    public string MonthDisplay => $"{Date.Month:d2}.{Date.Year:d4}";
    public string SortDisplay => $"{Date.Year:d4}.{Date.Month:d2}";
}