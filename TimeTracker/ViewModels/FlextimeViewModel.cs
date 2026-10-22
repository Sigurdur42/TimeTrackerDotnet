using System;
using ReactiveUI;
using TimeTracker.Models;

namespace TimeTracker.ViewModels;

public class FlextimeViewModel : ViewModelBase
{
    public const decimal DefaultHours = 8;

    private DateOnly _date = DateOnly.FromDateTime(DateTime.Now);
    private decimal? _hours = DefaultHours;

    public DateOnly Date
    {
        get => _date;
        set => this.RaiseAndSetIfChanged(ref _date, value);
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
