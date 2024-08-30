using System;

namespace TimeTracker.Models;

public class TimeRecord
{
    public DateOnly Date { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public bool AllOvertime { get; set; }
    public string? Comment { get; set; }
    public bool Travel { get; set; }
}