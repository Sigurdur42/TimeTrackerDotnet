using System.Collections.ObjectModel;
using TimeTracker.Models;

namespace TimeTracker.ViewModels;

// ReSharper disable once PartialTypeWithSinglePart
public partial class MainWindowViewModel : ViewModelBase
{
    private ObservableCollection<TimeRecord> _rawData = new();
}