using System.Collections.Specialized;
using TimeTracker.ViewModels;

namespace TimeTracker.Tests;

public class RangeObservableCollectionTests
{
    private static readonly Comparison<(int Key, string Value)> Descending = (a, b) => b.Key.CompareTo(a.Key);

    private static RangeObservableCollection<(int Key, string Value)> Create(params (int, string)[] items)
    {
        var collection = new RangeObservableCollection<(int Key, string Value)>();
        collection.Reset(items);
        return collection;
    }

    [Test]
    public void UpdateMergesWithoutReset()
    {
        var collection = Create((5, "a"), (4, "b"), (2, "c"), (1, "d"));
        var actions = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);

        (int, string)[] items = [(6, "new"), (5, "a"), (3, "x"), (2, "changed"), (1, "d")];
        collection.Update(items, Descending, (a, b) => a.Value == b.Value);

        Assert.Multiple(() =>
        {
            Assert.That(collection, Is.EqualTo(items));
            Assert.That(actions, Is.EquivalentTo(new[]
            {
                NotifyCollectionChangedAction.Add, NotifyCollectionChangedAction.Remove, NotifyCollectionChangedAction.Add,
                NotifyCollectionChangedAction.Replace
            }));
        });
    }

    [Test]
    public void UpdateWithSameContentRaisesNothing()
    {
        var collection = Create((2, "a"), (1, "b"));
        var raised = false;
        collection.CollectionChanged += (_, _) => raised = true;

        collection.Update([(2, "a"), (1, "b")], Descending, (a, b) => a.Value == b.Value);

        Assert.That(raised, Is.False);
    }

    [Test]
    public void UpdateRemovesTrailingItems()
    {
        var collection = Create((3, "a"), (2, "b"), (1, "c"));

        collection.Update([(3, "a")], Descending, (a, b) => a.Value == b.Value);

        Assert.That(collection, Is.EqualTo(new[] { (3, "a") }));
    }
}
