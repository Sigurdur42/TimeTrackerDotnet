using Avalonia;
using Avalonia.Media;

namespace TimeTracker;

public static class ApplicationBrushes
{
    public static SolidColorBrush? ControlForeground { get; private set; }
    public static SolidColorBrush? OverdueForeground { get; private set; }

    public static void Init()
    {
        var theme = Application.Current!.ActualThemeVariant;
        switch (theme.Key?.ToString()?.ToLowerInvariant())
        {
            default:
            case "light":
                ControlForeground = SolidColorBrush.Parse("Black");
                OverdueForeground = SolidColorBrush.Parse("OrangeRed");
                break;

            case "dark":
                ControlForeground = SolidColorBrush.Parse("White");
                OverdueForeground = SolidColorBrush.Parse("OrangeRed");
                break;
        }
    }
}