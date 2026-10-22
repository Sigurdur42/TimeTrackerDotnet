using Avalonia.Controls;
using Avalonia.Interactivity;
using TimeTracker.Models;

namespace TimeTracker.Views;

public partial class ConfirmDelete : Window
{
    public ConfirmDelete() : this(new TimeRecord())
    {
    }

    public ConfirmDelete(TimeRecord record)
    {
        InitializeComponent();
        DataContext = record;
    }

    public bool IsOk { get; private set; }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        IsOk = true;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        IsOk = false;
        Close(false);
    }
}
