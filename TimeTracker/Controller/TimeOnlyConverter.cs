using System;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace TimeTracker.Controller;

public class TimeOnlyConverter : DateOnlyConverter
{
    private readonly CultureInfo _culture = new CultureInfo("de-de");

    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (text is null)
        {
            return null;
        }
        
        var result = DateTime.ParseExact(text, "HH:mm", _culture);
        return TimeOnly.FromDateTime(result);
    }

    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        if (value is DateOnly date)
        {
            return date.ToString("HH:mm", _culture);
        }

        return null;
    }
}