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
            var prefix = "";
            if (timeSpan.TotalMinutes < 0) prefix = "-";

            var hours = decimal.Abs(timeSpan.Hours) + decimal.Abs(timeSpan.Days) * 24;
            var minutes = decimal.Abs(timeSpan.Minutes);

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