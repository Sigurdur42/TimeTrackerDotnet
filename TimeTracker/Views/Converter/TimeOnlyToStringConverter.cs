using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TimeTracker.Views.Converter;

public class TimeOnlyToStringConverter : IValueConverter
{
    private static readonly CultureInfo _culture = new("de-de");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeOnly timeOnly) return timeOnly.ToString("HH:mm", _culture);

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            var parts = text.Replace("_", "0").Split(':');
            var result = new TimeOnly(int.Parse(parts[0].Trim()), int.Parse(parts[1].Trim()));
            return result;
        }

        return null;
    }
}