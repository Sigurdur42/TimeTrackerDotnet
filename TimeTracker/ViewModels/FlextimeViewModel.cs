using System;
using ReactiveUI;
using TimeTracker.Models;

namespace TimeTracker.ViewModels;

public class FlextimeViewModel : ViewModelBase
{
    public const decimal DefaultHours = 8;

    private readonly Func<DateOnly, decimal> _defaultHours;
    private DateOnly _date = DateOnly.FromDateTime(DateTime.Now);
    private decimal? _hours;

    public FlextimeViewModel() : this(_ => DefaultHours)
    {
    }

    /// <param name="defaultHours">The hours proposed for a day, usually the daily target hours of that day</param>
    public FlextimeViewModel(Func<DateOnly, decimal> defaultHours)
    {
        _defaultHours = defaultHours;
        _hours = defaultHours(_date);
    }

    public DateOnly Date
    {
        get => _date;
        set
        {
            if (_date == value) return;

            // Follow the default of the new day unless the hours were changed by the user
            var keepsDefault = Hours == _defaultHours(_date);
            this.RaiseAndSetIfChanged(ref _date, value);
            if (keepsDefault) Hours = _defaultHours(value);
        }
    }

    public decimal? Hours
    {
        get => _hours;
        set => this.RaiseAndSetIfChanged(ref _hours, value);
    }

    public bool IsOk { get; set; }

    public TimeRecord CreateRecord()
    {
        return TimeRecord.CreateFlextime(Date, Hours ?? 0);
    }
}
