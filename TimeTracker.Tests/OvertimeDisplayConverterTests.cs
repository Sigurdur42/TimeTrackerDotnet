using TimeTracker.Views.Converter;

namespace TimeTracker.Tests;

public class OvertimeDisplayConverterTests
{
    [TestCase("01:04:00:000", "28:00")]
    [TestCase("04:00:000", "04:00")]
    [TestCase("-04:00:000", "-04:00")]
    public void VerifyConvert(string timeSpanText, string expectedOutput)
    {
        var timespan = TimeSpan.Parse(timeSpanText);
        var target = new OvertimeDisplayConverter();
        var actualOutput = target.Convert(timespan, null!, null, null!);
        Assert.That(actualOutput, Is.EqualTo(expectedOutput));
    }
}