using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TimeTrackerUi.ViewModels;
using TimeTrackerUi.Views;

namespace TimeTrackerUi;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        MainWindow? window = null;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainWindowViewModel();
            window = new MainWindow
            {
                DataContext = viewModel,
                ViewModel = viewModel,
            };

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
        window?.Change();
        
    }
}