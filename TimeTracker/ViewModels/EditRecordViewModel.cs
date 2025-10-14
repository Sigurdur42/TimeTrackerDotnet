using ReactiveUI;
using TimeTracker.Models;

namespace TimeTracker.ViewModels;

public class EditRecordViewModel : ViewModelBase
{
    private TimeRecord _timeRecord = new();

    // TODO: Bind to Text and convert afterwards

    public EditRecordViewModel(TimeRecord record, bool isEditMode)
    {
        OriginalTimeRecord = record;
        OriginalTimeRecord.CopyTo(_timeRecord);
        IsEditMode = isEditMode;
    }

    public TimeRecord TimeRecord
    {
        get => _timeRecord;
        set => this.RaiseAndSetIfChanged(ref _timeRecord, value);
    }

    public TimeRecord OriginalTimeRecord { get; }

    public bool IsEditMode { get; set; }
    public bool IsOk { get; set; }
}