using System;
using ReactiveUI;

namespace TimeTracker.Models;

public partial class TimeRecordByDay : ReactiveObject
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
}