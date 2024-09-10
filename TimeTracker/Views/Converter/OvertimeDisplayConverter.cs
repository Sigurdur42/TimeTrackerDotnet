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
            if (timeSpan.TotalMinutes < 0)
            {
                prefix = "-";
            }

            return timeSpan.Days > 0 ? $"{prefix}{timeSpan:dd\\:hh\\:mm}" : $"{prefix}{timeSpan:hh\\:mm}";
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
        // return null;
    }
}