using System;
using System.Globalization;
using CsvHelper;

namespace TimeTracker.Controller;

public sealed class TimeOnlyConverter : ICsvConverter<TimeOnly>
{
    private static readonly CultureInfo Culture = new("de-de");

    public TimeOnly Read(CsvReader reader, int index)
    {
        return ParseHoursMinutes(reader.GetString(index));
    }

    public void Write(CsvWriter writer, TimeOnly value)
    {
        writer.WriteField(value.ToString("HH:mm", Culture));
    }

    /// <summary>
    ///     Parses "H:mm" (any further ":ss" part is ignored) without allocating intermediate strings.
    /// </summary>
    internal static TimeOnly ParseHoursMinutes(ReadOnlySpan<char> text)
    {
        var separator = text.IndexOf(':');
        var hours = text[..separator];
        var minutes = text[(separator + 1)..];

        var nextSeparator = minutes.IndexOf(':');
        if (nextSeparator >= 0) minutes = minutes[..nextSeparator];

        return new TimeOnly(int.Parse(hours.Trim()), int.Parse(minutes.Trim()));
    }
}
