using System;

namespace TimeTracker.Models;

public class TimeRecordExcel
{
    public DateOnly Date { get; set; }
    public TimeOnly? Start { get; set; }
    public TimeOnly? End { get; set; }
    public TimeOnly? TravelStart { get; set; }
    public TimeOnly? TravelEnd { get; set; }
}