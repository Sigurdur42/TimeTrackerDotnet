using System;
using ReactiveUI;
using TimeTracker.Models;

namespace TimeTracker.ViewModels;

/// <summary>
///     Half a day sick: half of the target hours of the day are credited as working time
/// </summary>
public class SickDayViewModel : ViewModelBase
{
    private readonly Func<DateOnly, decimal> _creditedHours;
    private DateOnly _date = DateOnly.FromDateTime(DateTime.Now);

    public SickDayViewModel() : this(_ => FlextimeViewModel.DefaultHours / 2)
    {
    }

    /// <param name="creditedHours">The hours credited on a day, half of the daily target hours of that day</param>
    public SickDayViewModel(Func<DateOnly, decimal> creditedHours)
    {
        _creditedHours = creditedHours;
    }

    public DateOnly Date
    {
        get => _date;
        set
        {
            if (_date == value) return;
            this.RaiseAndSetIfChanged(ref _date, value);
            this.RaisePropertyChanged(nameof(Hours));
            this.RaisePropertyChanged(nameof(Warning));
        }
    }

    /// <summary>
    ///     Hours credited as working time
    /// </summary>
    public decimal Hours => _creditedHours(Date);

    /// <summary>
    ///     Warning about a day without target hours, null if the day is fine
    /// </summary>
    public string? Warning => Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
        ? "Weekends have no target hours, nothing is credited."
        : null;

    public bool IsOk { get; set; }

    public TimeRecord CreateRecord()
    {
        return TimeRecord.CreateSick(Date, Hours);
    }
}
