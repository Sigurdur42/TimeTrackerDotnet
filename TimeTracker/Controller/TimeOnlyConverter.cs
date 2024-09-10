using System;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace TimeTracker.Controller;

public class TimeOnlyConverter : DateOnlyConverter
{
    private readonly CultureInfo _culture = new("de-de");

    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (text is null)
        {
            return null;
        }
        
        var parts = text.Split(':');
        var result = new TimeOnly(int.Parse(parts[0].Trim()), int.Parse(parts[1].Trim()));
        // var result = TimeOnly.ParseExact(text, "HH:mm", _culture);
        return result;
    }

    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        if (value is TimeOnly date)
        {
            return date.ToString("HH:mm", _culture);
        }

        return null;
    }
}