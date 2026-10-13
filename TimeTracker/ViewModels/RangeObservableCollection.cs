using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TimeTracker.ViewModels;

/// <summary>
///     ObservableCollection that can replace its whole content with a single Reset notification
///     instead of one notification per added item.
/// </summary>
public class RangeObservableCollection<T> : ObservableCollection<T>
{
    public void Reset(IEnumerable<T> items)
    {
        CheckReentrancy();

        Items.Clear();
        foreach (var item in items) Items.Add(item);

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
