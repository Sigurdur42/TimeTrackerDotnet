using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace TimeTracker.Views.Converter;

public class TimeSpanToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Green = SolidColorBrush.Parse("Green");
    private static readonly SolidColorBrush Red = SolidColorBrush.Parse("OrangeRed");
    private static readonly SolidColorBrush Black = SolidColorBrush.Parse("Black");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TimeSpan timeSpan) return null;
        var totalMinutes = timeSpan.TotalMinutes;
        return totalMinutes switch
        {
            > 0 => Green,
            < 0 => Red,
            _ => Black
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}