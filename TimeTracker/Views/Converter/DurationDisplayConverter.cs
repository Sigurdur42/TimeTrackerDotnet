using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TimeTracker.Views.Converter;

public class DurationDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            var totalHours = timeSpan.TotalHours;
            return timeSpan.Days > 0 ? $"{timeSpan:dd\\:hh\\:mm} ({totalHours:F2})" : $"{timeSpan:hh\\:mm} ({totalHours:F2})";
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
        // return null;
    }
}