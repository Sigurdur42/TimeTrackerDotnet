using System;
using System.Collections.Generic;
using System.Diagnostics;
using ReactiveUI;

namespace TimeTracker.Models;

[DebuggerDisplay("{Date} - {Overtime}")]
public class TimeRecordByDay : ReactiveObject
{
    private DateOnly _date;
    private decimal _overtimeMinutes;
    private decimal _travelActiveMinutes;
    private decimal _travelPassiveMinutes;
    private decimal _workMinutes;
    private decimal _flextimeMinutes;
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

    public decimal FlextimeMinutes
    {
        get => _flextimeMinutes;
        set => this.RaiseAndSetIfChanged(ref _flextimeMinutes, value);
    }

    public TimeSpan OvertimeThisWeek
    {
        get => _overtimeThisWeek;
        set => this.RaiseAndSetIfChanged(ref _overtimeThisWeek, value);
    }

    /// <summary>
    ///     Work and active travel beyond the daily maximum, which is not counted
    /// </summary>
    public decimal WorkCapLossMinutes { get; set; }

    /// <summary>
    ///     Passive travel that is not counted due to its limitation
    /// </summary>
    public decimal PassiveTravelLossMinutes { get; set; }

    /// <summary>
    ///     Weekend surcharge added to the work and active travel
    /// </summary>
    public decimal SurchargeMinutes { get; set; }

    public bool IsOverMaxWorkTime => WorkCapLossMinutes > 0;

    public bool HasLoss => WorkCapLossMinutes > 0 || PassiveTravelLossMinutes > 0;

    public TimeSpan Loss => TimeSpan.FromMinutes((double)(WorkCapLossMinutes + PassiveTravelLossMinutes));

    public string? LossDescription
    {
        get
        {
            if (!HasLoss) return null;

            var lines = new List<string>();
            if (WorkCapLossMinutes > 0) lines.Add($"Work time over 10 h: {Format(WorkCapLossMinutes)} not counted");
            if (PassiveTravelLossMinutes > 0) lines.Add($"Travel (passive) limited: {Format(PassiveTravelLossMinutes)} not counted");
            return string.Join(Environment.NewLine, lines);

            static string Format(decimal minutes)
            {
                var time = TimeSpan.FromMinutes((double)minutes);
                return $"{(int)time.TotalHours:00}:{time.Minutes:00}";
            }
        }
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