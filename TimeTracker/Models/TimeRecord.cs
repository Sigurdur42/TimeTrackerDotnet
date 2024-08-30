using System;

namespace TimeTracker.Models;

public class TimeRecord : IEquatable<TimeRecord>
{
    public DateOnly Date { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public bool AllOvertime { get; set; }
    public string? Comment { get; set; }
    public bool Travel { get; set; }


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