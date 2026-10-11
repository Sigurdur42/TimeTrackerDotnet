using System;
using System.Globalization;
using Avalonia.Data.Converters;
using TimeTracker.Controller;

namespace TimeTracker.Views.Converter;

public class TimeOnlyToStringConverter : IValueConverter
{
    private static readonly CultureInfo Culture = new("de-de");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeOnly timeOnly) return timeOnly.ToString("HH:mm", Culture);

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            return TimeOnlyConverter.ParseHoursMinutes(text.Replace('_', '0'));
        }

        return null;
    }
}