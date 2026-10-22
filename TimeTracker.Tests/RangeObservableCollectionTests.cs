using System.Collections.Specialized;
using System.Reflection;
using TimeTracker.Controller;
using TimeTracker.Models;
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

    [Test]
    public void UpdateWithRecalculatedDataEqualsReset()
    {
        var calculator = new TimeRecordCalculator();
        var csvContent = new ResourceLoader().GetEmbeddedResourceString(Assembly.GetExecutingAssembly(), "DataFile.csv");
        var records = new TimeRecordSerializer().Deserialize(csvContent).ToList();

        var collection = new RangeObservableCollection<TimeRecordByDay>();
        collection.Reset(CalculateByDayDescending(records));

        // Edit a record, add a new day and remove all records of another day
        records[10].End = records[10].End.AddMinutes(30);
        records.Add(new TimeRecord { Date = new DateOnly(2030, 1, 7), Start = new TimeOnly(8, 0), End = new TimeOnly(17, 0) });
        var removedDate = records[100].Date;
        records.RemoveAll(_ => _.Date == removedDate);

        var expected = CalculateByDayDescending(records);
        collection.Update(expected, (a, b) => b.Date.CompareTo(a.Date), (a, b) =>
            a.OvertimeMinutes == b.OvertimeMinutes && a.OvertimeThisWeek == b.OvertimeThisWeek && a.IsLastOfWeek == b.IsLastOfWeek);

        Assert.That(collection.Select(_ => (_.Date, _.OvertimeMinutes, _.OvertimeThisWeek, _.IsLastOfWeek)),
            Is.EqualTo(expected.Select(_ => (_.Date, _.OvertimeMinutes, _.OvertimeThisWeek, _.IsLastOfWeek))));

        TimeRecordByDay[] CalculateByDayDescending(List<TimeRecord> data)
        {
            var byDay = calculator.CalculateByDay(data.ToArray());
            Array.Sort(byDay, (a, b) => a.Date.CompareTo(b.Date));
            calculator.CalculateWeeklyTotals(byDay);
            Array.Reverse(byDay);
            return byDay;
        }
    }
}
