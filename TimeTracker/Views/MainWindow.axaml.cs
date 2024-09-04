using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using TimeTracker.ViewModels;

namespace TimeTracker.Views;

public partial class MainWindow : Window
{
    internal MainWindowViewModel ViewModel { get; init; } = null!;

    public MainWindow()
    {
        
        InitializeComponent();
    }

    public void Change()
    {
        ViewModel.FileName = "changed desc";
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
        if (result.Any())
        {
            var selectedFile = result.First();
            ViewModel.FileName = selectedFile.Path.AbsolutePath; 
        }

        // // Set optional properties (adjust as needed)
        // dialog.Title = "Select Sata File";
        //
        // // TODO: Restore last file name
        // dialog.Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        // dialog.Filters.Add(new() { Name = "Data Files", Extensions = { "csv" } });
        // dialog.AllowMultiple = false;
        //
        // // Show the dialog and get the selected file(s)
        // var files = await dialog.ShowAsync(this);
        //
        // if (files is { Length: > 0 })
        // {
        //     // Process the selected file(s)
        //     var selectedFile = files[0];
        //     var filePath = selectedFile;
        //     Console.WriteLine(filePath);
        //
        //     _viewModel.FileName = filePath;
        // }
    }
}