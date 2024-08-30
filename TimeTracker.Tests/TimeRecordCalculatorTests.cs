using System.Globalization;
using System.Reflection;
using TimeTracker.Controller;
using TimeTracker.Models;

namespace TimeTracker.Tests;

public class TimeRecordCalculatorTests
{
    ResourceLoader _resourceLoader = null!;
    readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    readonly CultureInfo _culture = new ("de-de");
    TimeRecord[] _data = [];

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _resourceLoader = new ResourceLoader();
        var target = new TimeRecordSerializer();
        var csvContent = _resourceLoader.GetEmbeddedResourceString(_assembly, "DataFile.csv");

        _data = target.Deserialize(csvContent);
    }

    [TestCase("19.04.2023", "", 8 * 60 + 20, 0)]
    [TestCase("08.11.2023", "HJG Matheusz", 1 * 60 + 00, 0)]
    [TestCase("01.07.2024", "DUNA", 0, 10 * 60 + 00)]
    [TestCase("02.07.2024", "DUNA", 9 * 60 + 00, 2 * 60 + 00)]
    public void VerifyCategoryCalculation(string date, string category, decimal work, decimal travel)
    {
        var target = new TimeRecordCalculator();
        var result = target.CalculateCategorySummary(_data);

        var refDate = DateOnly.Parse(date, _culture);
        var found = result.First(_=> _.Date == refDate && _.Category == category);
        Assert.Multiple(() =>
        {
            Assert.That(found.WorkMinutes, Is.EqualTo(work), "Work");
            Assert.That(found.TravelMinutes, Is.EqualTo(travel), "Travel");
        });
    }
}