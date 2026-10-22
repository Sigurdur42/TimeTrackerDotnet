using TimeTracker.Views.Converter;

namespace TimeTracker.Tests;

public class OvertimeDisplayConverterTests
{
    [TestCase("01:04:00:000", "28:00")]
    [TestCase("04:00:000", "04:00")]
    [TestCase("-04:00:000", "-04:00")]
    [TestCase("-00:35:00", "-00:35")]
    [TestCase("-1.02:05:00", "-26:05")]
    [TestCase("00:00:00", "00:00")]
    [TestCase("-00:00:30", "-00:00")] // Below one minute only the sign is visible
    public void VerifyConvert(string timeSpanText, string expectedOutput)
    {
        var timespan = TimeSpan.Parse(timeSpanText);
        var target = new OvertimeDisplayConverter();
        var actualOutput = target.Convert(timespan, null!, null, null!);
        Assert.That(actualOutput, Is.EqualTo(expectedOutput));
    }
}