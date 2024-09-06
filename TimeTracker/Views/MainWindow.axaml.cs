using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TimeTracker.ViewModels;

namespace TimeTracker.Views;

public partial class MainWindow : Window
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
}