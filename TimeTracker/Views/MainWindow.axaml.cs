using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SukiUI.Controls;
using TimeTracker.Models;
using TimeTracker.ViewModels;

namespace TimeTracker.Views;

public partial class MainWindow : SukiWindow
{
    internal MainWindowViewModel ViewModel { get; init; } = null!;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnBrowseButtonClick(object? sender, RoutedEventArgs e)
    {
        var storageProvider = StorageProvider;

        // Create an OpenFilePickerOptions instance
        var options = new FilePickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "Select Sata File",
        };
        // options.FileTypeFilter.Add(new() { Name = "Data Files", Extensions = { "csv" } });

        var result = await storageProvider.OpenFilePickerAsync(options);
        if (!result.Any())
        {
            return;
        }

        var selectedFile = result[0];
        try
        {
            ViewModel.LoadDataFile(new FileInfo(selectedFile.Path.AbsolutePath));
        }
        catch (Exception exception)
        {
            // TODO: Better error handling
            Console.WriteLine(exception);
        }
    }

    private async void OnEditRecord(object? sender, RoutedEventArgs e)
    {
        var selected = ViewModel.SelectedDaiy;
        await ShowEditRecord(true, selected);
    }

    private async void OnNewRecord(object? sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        var last = ViewModel.RawData?.FirstOrDefault();
        var lastTime = last?.End ?? TimeOnly.FromDateTime(now);
        var newRecord = new TimeRecord()
        {
            Date = DateOnly.FromDateTime(now),
            Start = lastTime,
            End = lastTime,
        };

        await ShowEditRecord(false, newRecord);
    }

    private async Task ShowEditRecord(bool editMode, TimeRecord? record)
    {
        if (record is null)
        {
            return;
        }

        try
        {
            var dialog = new EditRecord(record, editMode);
            await dialog.ShowDialog(this);
            if (!dialog.IsOk)
            {
                return;
            }

            dialog.Data.CopyTo(record);
            if (!editMode)
            {
                ViewModel.RawData.Insert(0, record);
            }

            ViewModel.RecalculateAndSaveData();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}