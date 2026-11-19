using System;
using System.Collections.Generic;

namespace TimeTracker.Controller;

/// <summary>
///     Rules to credit the recorded time of a day, valid from <see cref="ValidFrom" /> until the next rule set starts.
///     A limit of null means no limit.
/// </summary>
public sealed record WorkingTimeRules
{
    /// <summary>
    ///     First day the rules apply
    /// </summary>
    public required DateOnly ValidFrom { get; init; }

    /// <summary>
    ///     Target working minutes of a weekday
    /// </summary>
    public required decimal DailyTargetMinutes { get; init; }

    /// <summary>
    ///     Maximum of work and active travel a day, the rest is lost
    /// </summary>
    public decimal? MaxWorkMinutes { get; init; }

    /// <summary>
    ///     Surcharge on work and active travel on Saturday, e.g. 0.25 for 25%
    /// </summary>
    public decimal SaturdaySurcharge { get; init; }

    /// <summary>
    ///     Surcharge on work and active travel on Sunday, e.g. 1 for 100%
    /// </summary>
    public decimal SundaySurcharge { get; init; }

    /// <summary>
    ///     Whether passive travel on a day without work and active travel is limited to <see cref="DailyTargetMinutes" />
    /// </summary>
    public bool LimitPassiveTravelWithoutWorkToTarget { get; init; }

    /// <summary>
    ///     Passive travel on a day with work counted fully, the rest is counted with <see cref="ReducedPassiveTravelFactor" />
    /// </summary>
    public decimal? FullPassiveTravelWithWorkMinutes { get; init; }

    /// <summary>
    ///     Factor of the passive travel beyond <see cref="FullPassiveTravelWithWorkMinutes" />
    /// </summary>
    public decimal ReducedPassiveTravelFactor { get; init; } = 1;

    public const double DefaultDailyTargetHours = 8;

    /// <summary>
    ///     First day of the rules introduced 01.12.2026
    /// </summary>
    public static readonly DateOnly Rules2026Start = new(2026, 12, 1);

    /// <summary>
    ///     The rules of the company, sorted by <see cref="ValidFrom" />
    /// </summary>
    /// <param name="dailyTargetHours">Target working hours of a weekday since <see cref="Rules2026Start" /></param>
    public static IReadOnlyList<WorkingTimeRules> CreateDefault(double dailyTargetHours = DefaultDailyTargetHours)
    {
        if (dailyTargetHours is <= 0 or > 24) throw new ArgumentOutOfRangeException(nameof(dailyTargetHours), dailyTargetHours, "Must be between 0 and 24 hours");

        return
        [
            // All recorded time counts, eight target hours
            new WorkingTimeRules
            {
                ValidFrom = DateOnly.MinValue,
                DailyTargetMinutes = 8 * 60
            },
            new WorkingTimeRules
            {
                ValidFrom = Rules2026Start,
                DailyTargetMinutes = (decimal)dailyTargetHours * 60,
                MaxWorkMinutes = 10 * 60,
                SaturdaySurcharge = 0.25m,
                SundaySurcharge = 1m,
                LimitPassiveTravelWithoutWorkToTarget = true,
                FullPassiveTravelWithWorkMinutes = 6 * 60,
                ReducedPassiveTravelFactor = 0.5m
            }
        ];
    }
}
