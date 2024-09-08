using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TimeTracker.Views.Converter;

public class DateOnlyDateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateOnly dateOnly)
        {
            return new DateTimeOffset(new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day));
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset date)
        {
            return DateOnly.FromDateTime(date.DateTime);
        }

        return null;
    }
}