using System;
using System.Runtime.InteropServices.JavaScript;
using CsvHelper.Configuration.Attributes;
using ReactiveUI;

namespace TimeTracker.Models;

public partial class TimeRecord : ReactiveObject, IEquatable<TimeRecord>
{
    private DateOnly _date;
    private TimeOnly _start;
    private TimeOnly _end;
    private bool _allOvertime;
    private bool _travel;
    private string? _comment;

    [Index(0)]
    public DateOnly Date
    {
        get => _date;
        set => this.RaiseAndSetIfChanged(ref _date, value);
    }

    [Index(1)]
    public TimeOnly Start
    {
        get => _start;
        set => this.RaiseAndSetIfChanged(ref _start, value);
    }

    [Index(2)]
    public TimeOnly End
    {
        get => _end;
        set => this.RaiseAndSetIfChanged(ref _end, value);
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
        set => this.RaiseAndSetIfChanged(ref _comment, value);
    }

    [Ignore] public TimeSpan Duration => End - Start;

    [Ignore]
    public string Category
    {
        get
        {
            var comment = Comment?.Trim();
            if (comment is null || comment.Length == 0)
            {
                return "";
            }

            var index = comment.IndexOf(':');
            if (index > 0)
            {
                comment = comment[..index].Trim();
            }

            return $"{char.ToUpper(comment[0])}{comment[1..]}";
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
        return Date.Equals(other.Date)
               && Start.Equals(other.Start)
               && End.Equals(other.End) &&
               AllOvertime == other.AllOvertime
               && Comment == other.Comment
               && Travel == other.Travel;
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