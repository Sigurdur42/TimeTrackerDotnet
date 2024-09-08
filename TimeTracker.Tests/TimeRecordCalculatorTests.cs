using System.Globalization;
using System.Reflection;
using TimeTracker.Controller;
using TimeTracker.Models;

namespace TimeTracker.Tests;

public class TimeRecordCalculatorTests
{
    ResourceLoader _resourceLoader = null!;
    readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    readonly CultureInfo _culture = new("de-de");
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
        var found = result.First(_ => _.Date == refDate && _.Category == category);
        Assert.Multiple(() =>
        {
            Assert.That(found.WorkMinutes, Is.EqualTo(work), "Work");
            Assert.That(found.TravelMinutes, Is.EqualTo(travel), "Travel");
        });
    }

    [TestCase("29.05.2024", -1 * (1 * 60 + 10))]
    [TestCase("01.07.2024", 2 * 60 + 00)]
    [TestCase("27.01.2024", 7 * 60 + 00)]
    [TestCase("17.02.2024", 10 * 60 + 20)]
    public void VerifyByDayCalculation(string date, decimal overtime)
    {
        var target = new TimeRecordCalculator();
        var result = target.CalculateByDay(_data);

        var refDate = DateOnly.Parse(date, _culture);
        var found = result.First(_ => _.Date == refDate);
        Assert.That(found.OvertimeMinutes, Is.EqualTo(overtime), "overtime");
    }

    [TestCase("07.2024", 14 * 60 + 55)]
    [TestCase("05.2024", -1 * (16 * 60 + 05))]
    [TestCase("12.2023", -1 * (16 * 60 + 05))]
    public void VerifyByMonthCalculation(string month, decimal overtime)
    {
        var target = new TimeRecordCalculator();
        var result = target.CalculateByMonth(_data);

        var found = result.First(_ => _.MonthDisplay == month);
        Assert.That(found.OvertimeMinutes, Is.EqualTo(overtime), "overtime");
    }

    [TestCase("2023.04", -65)]
    [TestCase("2024.01", -65)]
    public void VerifyTotalOvertimeCalculation(string date, decimal overtime)
    {
        var target = new TimeRecordCalculator();
        var result = target.CalculateByMonth(_data);

        var filtered = result.Where(_ => string.Compare(_.SortDisplay, date, StringComparison.Ordinal) <= 0).ToArray();
        var total = target.CalculateTotalOvertime(filtered);
        Assert.That(total, Is.EqualTo(overtime));
    }
}