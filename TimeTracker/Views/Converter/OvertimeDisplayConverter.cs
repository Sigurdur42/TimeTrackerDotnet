using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TimeTracker.Views.Converter;

public class OvertimeDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            var prefix = timeSpan.Ticks < 0 ? "-" : "";

            var hours = Math.Abs(timeSpan.Hours) + Math.Abs(timeSpan.Days) * 24;
            var minutes = Math.Abs(timeSpan.Minutes);

            return $"{prefix}{hours:00}:{minutes:00}";
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
        // return null;
    }
}