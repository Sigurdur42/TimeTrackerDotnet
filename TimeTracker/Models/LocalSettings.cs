using Config.Net;

namespace TimeTracker.Models;

public interface ILocalSettings
{
    string? LastDataFile { get; set; }

    /// <summary>
    ///     Target working hours of a weekday (rules since 01.12.2026)
    /// </summary>
    [Option(DefaultValue = 8.0)]
    double DailyTargetHours { get; set; }
}