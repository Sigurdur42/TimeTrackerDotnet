using System.Globalization;
using System.Reflection;
using TimeTracker.Controller;
using TimeTracker.Models;

namespace TimeTracker.Tests;

public class CsvConfigurationTests
{
    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private readonly CultureInfo _culture = new("de-de");
    private ResourceLoader _resourceLoader = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _resourceLoader = new ResourceLoader();
    }

    [Test]
    public void TestLoadAndWriteCsv()
    {
        // load reference data
        var target = new TimeRecordSerializer();
        var csvContent = _resourceLoader.GetEmbeddedResourceString(_assembly, "DataFile.csv");

        var referenceData = target.Deserialize(csvContent);

        // Now write it to hard disc
        var writtenData = target.Serialize(referenceData);

        // read that again
        var reloadedData = target.Deserialize(writtenData);

        // Now verify that all objects are equal
        Assert.That(referenceData, Is.EqualTo(reloadedData));
    }

    [Test]
    public void SerializedOutputMatchesExistingFileFormat()
    {
        var target = new TimeRecordSerializer();
        var csvContent = _resourceLoader.GetEmbeddedResourceString(_assembly, "DataFile.csv").Replace("\r\n", "\n");

        var writtenData = target.Serialize(target.Deserialize(csvContent));

        // Files without the passive travel column only get an empty field appended
        var expected = csvContent.Replace("\n", ";\n");
        Assert.That(writtenData, Is.EqualTo(expected));
    }

    [Test]
    public void FileWithoutPassiveTravelColumnIsRead()
    {
        var result = new TimeRecordSerializer().Deserialize("05.02.2024;08:00;10:00;;misc;True\n");

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Length.EqualTo(1));
            Assert.That(result[0].TravelActive, Is.True);
            Assert.That(result[0].TravelPassive, Is.False);
        });
    }

    [Test]
    public void PassiveTravelIsStoredInSeventhColumn()
    {
        var target = new TimeRecordSerializer();
        var record = new TimeRecord
        {
            Date = new DateOnly(2024, 2, 5),
            Start = new TimeOnly(8, 0),
            End = new TimeOnly(10, 0),
            Comment = "misc",
            TravelPassive = true
        };

        var written = target.Serialize([record]);
        var reloaded = target.Deserialize(written);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo("05.02.2024;08:00;10:00;;misc;;True\n"));
            Assert.That(reloaded, Is.EqualTo(new[] { record }));
        });
    }

    [Test]
    public void TestLoadDataFile()
    {
        var target = new TimeRecordSerializer();
        var csvContent = _resourceLoader.GetEmbeddedResourceString(_assembly, "DataFile.csv");

        var result = target.Deserialize(csvContent);
        Assert.That(result, Is.Not.Null);

        Assert.Multiple(() =>
        {
            VerifyTimeRecord(result, "22.02.2024", new TimeOnly(0, 0), new TimeOnly(19, 0), false, false, "VPS:Reise");

            VerifyTimeRecord(result, "01.06.2024", new TimeOnly(8, 45), new TimeOnly(12, 45), false, true, "Führungskräftetraining");
        });
    }

    private void VerifyTimeRecord(TimeRecord[] timeRecords, string date, TimeOnly start, TimeOnly end, bool isTravel, bool isOAllOvertime, string comment)
    {
        var search = DateOnly.Parse(date, _culture);
        var found = timeRecords.First(_ => _.Date == search);

        Assert.Multiple(() =>
        {
            Assert.That(found.Start, Is.EqualTo(start));
            Assert.That(found.End, Is.EqualTo(end));
            Assert.That(found.Comment, Is.EqualTo(comment));
            Assert.That(found.TravelActive, Is.EqualTo(isTravel), "Travel");
            Assert.That(found.AllOvertime, Is.EqualTo(isOAllOvertime), @"All Overtime");
        });
    }
}