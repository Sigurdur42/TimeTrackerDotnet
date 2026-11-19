using System.ComponentModel;
using ReactiveUI;
using TimeTracker.Models;

namespace TimeTracker.ViewModels;

public class EditRecordViewModel : ViewModelBase
{
    private readonly TimeRecord _timeRecord = new();
    private bool _isConfirmationPending;

    public EditRecordViewModel(TimeRecord record, bool isEditMode)
    {
        OriginalTimeRecord = record;
        OriginalTimeRecord.CopyTo(_timeRecord);
        IsEditMode = isEditMode;

        _timeRecord.PropertyChanged += OnTimeRecordChanged;
    }

    public TimeRecord TimeRecord => _timeRecord;

    public TimeRecord OriginalTimeRecord { get; }

    public bool IsEditMode { get; set; }
    public bool IsOk { get; set; }

    /// <summary>
    ///     Warning about implausible times, null if the times are fine
    /// </summary>
    public string? Warning
    {
        get
        {
            var start = TimeRecord.Start;
            var end = TimeRecord.End;
            if (end == start) return "Start and end are equal, the duration is 0.";
            if (end < start) return $"End is before start: counted as {TimeRecord.Duration:hh\\:mm} h over midnight.";
            return null;
        }
    }

    /// <summary>
    ///     True after OK was pressed once despite a <see cref="Warning" />
    /// </summary>
    public bool IsConfirmationPending
    {
        get => _isConfirmationPending;
        private set => this.RaiseAndSetIfChanged(ref _isConfirmationPending, value);
    }

    /// <summary>
    ///     Called when OK is pressed. With a <see cref="Warning" /> OK must be pressed a second time to confirm.
    /// </summary>
    /// <returns>True if the record may be saved</returns>
    public bool Confirm()
    {
        if (Warning is null || IsConfirmationPending) return true;

        IsConfirmationPending = true;
        return false;
    }

    private void OnTimeRecordChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(TimeRecord.Start) or nameof(TimeRecord.End))) return;

        // A changed time needs a new confirmation
        IsConfirmationPending = false;
        this.RaisePropertyChanged(nameof(Warning));
    }
}
