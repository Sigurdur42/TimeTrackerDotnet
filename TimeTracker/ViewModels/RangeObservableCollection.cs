using System;
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

    /// <summary>
    ///     Merges the new content into the collection so that only changed items raise notifications. Both the current
    ///     content and <paramref name="items" /> must be sorted by <paramref name="order" />. Unchanged items are kept,
    ///     changed ones are replaced, missing ones removed and new ones inserted. This keeps a bound grid from
    ///     regenerating all its rows and losing its scroll position.
    /// </summary>
    public void Update(IReadOnlyList<T> items, Comparison<T> order, Func<T, T, bool> sameValues)
    {
        if (Count == 0)
        {
            Reset(items);
            return;
        }

        var index = 0;
        foreach (var item in items)
        {
            while (index < Count && order(this[index], item) < 0) RemoveAt(index);

            if (index < Count && order(this[index], item) == 0)
            {
                if (!sameValues(this[index], item)) this[index] = item;
            }
            else
            {
                Insert(index, item);
            }

            index++;
        }

        while (index < Count) RemoveAt(Count - 1);
    }
}
