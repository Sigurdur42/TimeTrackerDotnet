using System;
using System.Diagnostics;
using CsvHelper;
using ReactiveUI;
using TimeTracker.Controller;

namespace TimeTracker.Models;

[CsvRecord]
[CsvIgnoreBase]
[DebuggerDisplay("{Date} - {Start} - {End} - {Duration}")]
public partial class TimeRecord : ReactiveObject, IEquatable<TimeRecord>
{
    /// <summary>
    ///     Comment (category) of records that reduce the overtime instead of being working time
    /// </summary>
    public const string FlextimeCategory = "Gleitzeit";

    /// <summary>
    ///     Comment (category) of records that credit time of sickness on a weekday
    /// </summary>
    public const string SickCategory = "Krank";

    private bool _allOvertime;
    private string? _comment;
    private DateOnly _date;
    private TimeOnly _end;
    private TimeOnly _start;
    private bool _travelActive;
    private bool _travelPassive;

    [CsvColumn(Index = 0)]
    [CsvFormat("dd.MM.yyyy", "d.M.yyyy")]
    public DateOnly Date
    {
        get => _date;
        set
        {
            if (_date == value) return;
            this.RaiseAndSetIfChanged(ref _date, value);
            this.RaisePropertyChanged(nameof(Month));
        }
    }

    [CsvColumn(Index = 1)]
    [CsvConverter(typeof(TimeOnlyConverter))]
    public TimeOnly Start
    {
        get => _start;
        set
        {
            if (_start == value) return;
            this.RaiseAndSetIfChanged(ref _start, value);
            this.RaisePropertyChanged(nameof(Duration));
        }
    }

    [CsvColumn(Index = 2)]
    [CsvConverter(typeof(TimeOnlyConverter))]
    public TimeOnly End
    {
        get => _end;
        set
        {
            if (_end == value) return;
            this.RaiseAndSetIfChanged(ref _end, value);
            this.RaisePropertyChanged(nameof(Duration));
        }
    }

    [CsvColumn(Index = 3)]
    [CsvConverter(typeof(PythonBooleanConverter))]
    public bool AllOvertime
    {
        get => _allOvertime;
        set => this.RaiseAndSetIfChanged(ref _allOvertime, value);
    }

    /// <summary>
    ///     Active travel time. A record is either active or passive travel, never both.
    /// </summary>
    [CsvColumn(Index = 5)]
    [CsvConverter(typeof(PythonBooleanConverter))]
    public bool TravelActive
    {
        get => _travelActive;
        set
        {
            this.RaiseAndSetIfChanged(ref _travelActive, value);
            if (value) TravelPassive = false;
        }
    }

    /// <summary>
    ///     Passive travel time. A record is either active or passive travel, never both.
    /// </summary>
    [CsvColumn(Index = 6)]
    [CsvConverter(typeof(PythonBooleanConverter))]
    public bool TravelPassive
    {
        get => _travelPassive;
        set
        {
            this.RaiseAndSetIfChanged(ref _travelPassive, value);
            if (value) TravelActive = false;
        }
    }

    [CsvColumn(Index = 4)]
    public string? Comment
    {
        get => _comment;
        set
        {
            if (_comment == value) return;
            this.RaiseAndSetIfChanged(ref _comment, value);
            this.RaisePropertyChanged(nameof(Category));
        }
    }

    [CsvIgnore] public TimeSpan Duration => End - Start;

    [CsvIgnore]
    public string Category
    {
        get
        {
            var comment = Comment.AsSpan().Trim();
            if (comment.IsEmpty) return "";

            if (comment.StartsWith("gmk", StringComparison.InvariantCultureIgnoreCase))
                // GMK shall not be summarized as it needs to be stated separately
                return comment.ToString();

            var index = comment.IndexOf(':');
            if (index > 0) comment = comment[..index].Trim();

            var first = char.ToUpper(comment[0]);
            return string.Concat(new ReadOnlySpan<char>(in first), comment[1..]);
        }
    }

    [CsvIgnore] public bool IsFlextime => string.Equals(Category, FlextimeCategory, StringComparison.OrdinalIgnoreCase);

    [CsvIgnore] public bool IsSick => string.Equals(Category, SickCategory, StringComparison.OrdinalIgnoreCase);

    [CsvIgnore]
    public string Month
    {
        get
        {
            var date = Date;
            return $"{date.Month:d2}.{date.Year:d4}";
        }
    }

    public bool Equals(TimeRecord? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Date.Equals(other.Date) && Start.Equals(other.Start) && End.Equals(other.End) && AllOvertime == other.AllOvertime && Comment == other.Comment && TravelActive == other.TravelActive && TravelPassive == other.TravelPassive;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((TimeRecord)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Date, Start, End, AllOvertime, Comment, TravelActive, TravelPassive);
    }

    /// <summary>
    ///     Creates a record for overtime taken off. The hours are stored as duration starting at midnight.
    /// </summary>
    public static TimeRecord CreateFlextime(DateOnly date, decimal hours)
    {
        return CreateDuration(date, hours, FlextimeCategory);
    }

    /// <summary>
    ///     Creates a record for time of sickness credited as working time, e.g. half of the target hours for half a day
    ///     sick. The hours are stored as duration starting at midnight.
    /// </summary>
    public static TimeRecord CreateSick(DateOnly date, decimal hours)
    {
        return CreateDuration(date, hours, SickCategory);
    }

    private static TimeRecord CreateDuration(DateOnly date, decimal hours, string category)
    {
        return new TimeRecord
        {
            Date = date,
            Start = TimeOnly.MinValue,
            End = TimeOnly.MinValue.AddMinutes((double)Math.Round(hours * 60)),
            Comment = category
        };
    }

    public void CopyTo(TimeRecord other)
    {
        other.Date = Date;
        other.Start = Start;
        other.End = End;
        other.Comment = Comment;
        other.TravelActive = TravelActive;
        other.TravelPassive = TravelPassive;
        other.AllOvertime = AllOvertime;
    }
}