using ReactiveUI;

namespace TimeTrackerUi.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    
    private string _fileName = string.Empty;



    public string FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }
}