using System;
using System.Diagnostics;
using ReactiveUI;

namespace TimeTracker.Models;

[DebuggerDisplay("{Date} - {Overtime}")]
public class TimeRecordByDay : ReactiveObject
{
    private DateOnly _date;
    private decimal _overtimeMinutes;
    private decimal _travelMinutes;
    private decimal _workMinutes;
    private TimeSpan _overtimeThisWeek;

    public TimeOnly Start { get; set; }

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

    public TimeSpan OvertimeThisWeek
    {
        get => _overtimeThisWeek;
        set => this.RaiseAndSetIfChanged(ref _overtimeThisWeek, value);
    }

    public TimeSpan Overtime => TimeSpan.FromMinutes((double)OvertimeMinutes);

    public string Month
    {
        get
        {
            var date = Date;
            return $"{date.Month:d2}.{date.Year:d4}";
        }
    }

    private bool _isLastOfWeek;

    public bool IsLastOfWeek
    {
        get => _isLastOfWeek;
        set => this.RaiseAndSetIfChanged(ref _isLastOfWeek, value);
    }
}