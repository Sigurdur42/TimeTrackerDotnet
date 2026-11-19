using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TimeTracker.Models;
using TimeTracker.ViewModels;

namespace TimeTracker.Views;

public partial class SickDay : Window
{
    private readonly SickDayViewModel _viewModel;

    public SickDay() : this(_ => FlextimeViewModel.DefaultHours / 2)
    {
    }

    /// <param name="creditedHours">The hours credited on a day, half of the daily target hours of that day</param>
    public SickDay(Func<DateOnly, decimal> creditedHours)
    {
        InitializeComponent();
        _viewModel = new SickDayViewModel(creditedHours);
        DataContext = _viewModel;
    }

    public bool IsOk => _viewModel.IsOk;

    public TimeRecord CreateRecord()
    {
        return _viewModel.CreateRecord();
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        _viewModel.IsOk = true;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        _viewModel.IsOk = false;
        Close(false);
    }
}
