using System;

namespace TimeTracker.Models;

public class TimeRecordExcel
{
    public DateOnly Date { get; set; }
    public TimeOnly? Start { get; set; }
    public TimeOnly? End { get; set; }
    public TimeOnly? TravelActiveStart { get; set; }
    public TimeOnly? TravelActiveEnd { get; set; }
    public TimeOnly? TravelPassiveStart { get; set; }
    public TimeOnly? TravelPassiveEnd { get; set; }
}