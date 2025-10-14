using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TimeTracker.Views.Converter;

public class TimeOnlyTimeSpanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeOnly timeOnly) return TimeSpan.FromTicks(timeOnly.Ticks);

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan) return TimeOnly.FromTimeSpan(timeSpan);

        return null;
    }
}