using System;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace TimeTracker.Controller;

public class TimeOnlyConverter : DateOnlyConverter
{
    private static readonly CultureInfo Culture = new("de-de");

    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (text is null) return null;

        return ParseHoursMinutes(text);
    }

    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        if (value is TimeOnly date) return date.ToString("HH:mm", Culture);

        return null;
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
