using Avalonia.Controls;
using Avalonia.Interactivity;
using TimeTracker.Models;
using TimeTracker.ViewModels;

namespace TimeTracker.Views;

public partial class EditRecord : Window
{
    private readonly EditRecordViewModel _viewModel;

    public bool IsOk => _viewModel.IsOk;
    public TimeRecord Data => _viewModel.TimeRecord;

    public EditRecord(TimeRecord record, bool isEdit)
    {
        InitializeComponent();
        _viewModel = new EditRecordViewModel(record, isEdit);
        DataContext = _viewModel;
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