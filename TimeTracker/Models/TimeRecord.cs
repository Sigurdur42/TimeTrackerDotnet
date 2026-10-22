using System;
using System.Diagnostics;
using CsvHelper.Configuration.Attributes;
using ReactiveUI;

namespace TimeTracker.Models;

[DebuggerDisplay("{Date} - {Start} - {End} - {Duration}")]
public class TimeRecord : ReactiveObject, IEquatable<TimeRecord>
{
    private bool _allOvertime;
    private string? _comment;
    private DateOnly _date;
    private TimeOnly _end;
    private TimeOnly _start;
    private bool _travel;

    [Index(0)]
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

    [Index(1)]
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

    [Index(2)]
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

    [Index(3)]
    public bool AllOvertime
    {
        get => _allOvertime;
        set => this.RaiseAndSetIfChanged(ref _allOvertime, value);
    }

    [Index(5)]
    public bool Travel
    {
        get => _travel;
        set => this.RaiseAndSetIfChanged(ref _travel, value);
    }

    [Index(4)]
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

    [Ignore] public TimeSpan Duration => End - Start;

    [Ignore]
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

    [Ignore]
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
        return Date.Equals(other.Date) && Start.Equals(other.Start) && End.Equals(other.End) && AllOvertime == other.AllOvertime && Comment == other.Comment && Travel == other.Travel;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((TimeRecord)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Date, Start, End, AllOvertime, Comment, Travel);
    }

    public void CopyTo(TimeRecord other)
    {
        other.Date = Date;
        other.Start = Start;
        other.End = End;
        other.Comment = Comment;
        other.Travel = Travel;
        other.AllOvertime = AllOvertime;
    }
}