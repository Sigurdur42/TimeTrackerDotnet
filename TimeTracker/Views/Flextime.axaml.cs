using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TimeTracker.Models;
using TimeTracker.ViewModels;

namespace TimeTracker.Views;

public partial class Flextime : Window
{
    private readonly FlextimeViewModel _viewModel;

    public Flextime() : this(_ => FlextimeViewModel.DefaultHours)
    {
    }

    /// <param name="defaultHours">The hours proposed for a day, usually the daily target hours of that day</param>
    public Flextime(Func<DateOnly, decimal> defaultHours)
    {
        InitializeComponent();
        _viewModel = new FlextimeViewModel(defaultHours);
        DataContext = _viewModel;
    }

    public bool IsOk => _viewModel.IsOk;

    public TimeRecord CreateRecord()
    {
        return _viewModel.CreateRecord();
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.Hours is not > 0) return;

        _viewModel.IsOk = true;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        _viewModel.IsOk = false;
        Close(false);
    }
}
