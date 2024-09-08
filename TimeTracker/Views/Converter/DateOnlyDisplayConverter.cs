using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TimeTracker.Views.Converter;

public class DateOnlyDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateOnly date)
        {
            return date.ToString("dd.MM.yyyy");
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}