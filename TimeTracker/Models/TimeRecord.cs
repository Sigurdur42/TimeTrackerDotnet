using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper.Configuration.Attributes;

namespace TimeTracker.Models;

public partial class TimeRecord : ObservableObject, IEquatable<TimeRecord>
{
    [ObservableProperty] DateOnly _date;
    [ObservableProperty] TimeOnly _start;
    [ObservableProperty] private TimeOnly _end;
    [ObservableProperty] private bool _allOvertime;
    [ObservableProperty] private string? _comment;
    [ObservableProperty] private bool _travel;

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
}