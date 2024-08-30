using TimeTracker.Models;

namespace TimeTracker.Tests;

public class TimeRecordTests
{
    [TestCase("", "")]
    [TestCase(null, "")]
    [TestCase("  ", "")]
    [TestCase("misc", "Misc")]
    [TestCase("misc: some tasks", "Misc")]
    [TestCase("DUNA: Travel", "DUNA")]
    [TestCase("DUNA", "DUNA")]
    [TestCase("DUNA ", "DUNA")]
    public void CategoryTests(string comment, string expectedCategory)
    {
        var target = new TimeRecord
        {
            Comment = comment
        };

        var result = target.Category;
        Assert.That(expectedCategory, Is.EqualTo(result));
    }
}