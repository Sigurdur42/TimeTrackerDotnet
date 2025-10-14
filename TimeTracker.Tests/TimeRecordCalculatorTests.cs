using System.Globalization;
using System.Reflection;
using TimeTracker.Controller;
using TimeTracker.Models;

namespace TimeTracker.Tests;

public class TimeRecordCalculatorTests
{
    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private readonly CultureInfo _culture = new("de-de");
    private TimeRecord[] _data = [];
    private ResourceLoader _resourceLoader = null!;

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
    [TestCase("29.08.2024", -1 * 50)]
    [TestCase("29.12.2023", -1 * 8 * 60)]
    [TestCase("22.12.2023", -1 * (1 * 60 + 40))]
    [TestCase("21.12.2023", -1 * (2 * 60 + 25))]
    [TestCase("20.12.2023", 1 * (0 * 60 + 5))]
    [TestCase("19.12.2023", -1 * (1 * 60 + 10))]
    [TestCase("18.12.2023", 1 * (0 * 60 + 40))]
    [TestCase("15.12.2023", -1 * (3 * 60 + 40))]
    [TestCase("14.12.2023", 1 * (2 * 60 + 30))]
    [TestCase("13.12.2023", -1 * (0 * 60 + 30))]
    [TestCase("12.12.2023", 1 * (0 * 60 + 55))]
    [TestCase("11.12.2023", 1 * (0 * 60 + 40))]
    [TestCase("08.12.2023", -1 * (0 * 60 + 50))]
    [TestCase("07.12.2023", 1 * (1 * 60 + 30))]
    [TestCase("06.12.2023", 1 * (0 * 60 + 30))]
    [TestCase("05.12.2023", -1 * (0 * 60 + 55))]
    [TestCase("04.12.2023", 1 * (1 * 60 + 05))]
    [TestCase("01.12.2023", -1 * (2 * 60 + 40))]
    public void VerifyByDayCalculation(string date, decimal overtime)
    {
        var target = new TimeRecordCalculator();
        var result = target.CalculateByDay(_data);

        var refDate = DateOnly.Parse(date, _culture);
        var found = result.First(_ => _.Date == refDate);
        Assert.That(overtime, Is.EqualTo(found.OvertimeMinutes), "overtime");
    }

    [TestCase("01.02.2024", "1:55")]
    [TestCase("02.02.2024", "-3:45")]
    [TestCase("05.02.2024", "-0:35")]
    [TestCase("06.02.2024", "0:40")]
    [TestCase("07.02.2024", "0:55")]
    [TestCase("08.02.2024", "0:40")]
    [TestCase("09.02.2024", "-1:25")]
    [TestCase("12.02.2024", "4:59")]
    [TestCase("13.02.2024", "0:30")]
    [TestCase("14.02.2024", "2:00")]
    [TestCase("15.02.2024", "2:15")]
    [TestCase("16.02.2024", "2:45")]
    [TestCase("17.02.2024", "10:20")]
    [TestCase("18.02.2024", "14:00")]
    [TestCase("19.02.2024", "0:00")]
    [TestCase("20.02.2024", "0:00")]
    [TestCase("21.02.2024", "5:14")]
    [TestCase("22.02.2024", "11:00")]
    [TestCase("23.02.2024", "-6:30")]
    [TestCase("26.02.2024", "1:05")]
    [TestCase("27.02.2024", "-2:00")]
    [TestCase("28.02.2024", "1:05")]
    [TestCase("29.02.2024", "1:20")]
    [TestCase("30.08.2024", "-0:45")]
    [TestCase("01.08.2023", "-0:10")]
    [TestCase("02.08.2023", "0:40")]
    [TestCase("03.08.2023", "1:00")]
    [TestCase("04.08.2023", "0:30")]
    [TestCase("08.08.2023", "2:30")]
    [TestCase("21.08.2023", "0:45")]
    [TestCase("22.08.2023", "0:30")]
    [TestCase("23.08.2023", "-1:40")]
    [TestCase("24.08.2023", "-0:40")]
    [TestCase("25.08.2023", "-1:40")]
    [TestCase("28.08.2023", "1:10")]
    [TestCase("29.08.2023", "0:35")]
    [TestCase("30.08.2023", "0:06")]
    public void VerifyByDayCalculationReadable(string date, string overtimeReadable)
    {
        var overtime = (decimal)TimeSpan.Parse(overtimeReadable).TotalMinutes;
        var target = new TimeRecordCalculator();
        var result = target.CalculateByDay(_data);

        var refDate = DateOnly.Parse(date, _culture);
        var found = result.First(_ => _.Date == refDate);
        Assert.That(found.OvertimeMinutes, Is.EqualTo(overtime), "overtime");
    }

    [TestCase("07.2024", 14 * 60 + 55)]
    [TestCase("05.2024", -1 * (16 * 60 + 05))]
    [TestCase("12.2023", -1 * (13 * 60 + 55))]
    [TestCase("02.2024", 46 * 60 + 28)]
    [TestCase("08.2023", 3 * 60 + 21)]
    public void VerifyByMonthCalculation(string month, decimal overtime)
    {
        var target = new TimeRecordCalculator();
        var byDay = target.CalculateByDay(_data);
        var result = target.CalculateByMonth(byDay);

        var found = result.First(_ => _.MonthDisplay == month);
        Assert.That(found.OvertimeMinutes, Is.EqualTo(overtime), "overtime");
    }

    [TestCase("2023.04", -835)]
    [TestCase("2024.01", -65)]
    public void VerifyTotalOvertimeCalculation(string date, decimal overtime)
    {
        var target = new TimeRecordCalculator();
        var byDay = target.CalculateByDay(_data);
        var result = target.CalculateByMonth(byDay);

        var filtered = result.Where(_ => string.Compare(_.SortDisplay, date, StringComparison.Ordinal) <= 0).ToArray();
        var total = target.CalculateTotalOvertime(filtered);
        Assert.That(total, Is.EqualTo(overtime));
    }
}