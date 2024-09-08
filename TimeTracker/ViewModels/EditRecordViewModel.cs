using ReactiveUI;
using TimeTracker.Models;

namespace TimeTracker.ViewModels;

public class EditRecordViewModel : ViewModelBase
{
    private readonly TimeRecord _original;
    private TimeRecord _timeRecord = new();

    public EditRecordViewModel(TimeRecord record, bool isEditMode)
    {
        _original = record;
        _original.CopyTo(_timeRecord);
        IsEditMode = isEditMode;
    }

    public TimeRecord TimeRecord
    {
        get => _timeRecord;
        set => this.RaiseAndSetIfChanged(ref _timeRecord, value);
    }
    
    public TimeRecord OriginalTimeRecord => _original;

    public bool IsEditMode { get; set; }
    public bool IsOk { get; set; }
}