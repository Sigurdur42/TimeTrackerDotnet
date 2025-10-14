using System.Globalization;
using System.Reflection;

namespace TimeTracker.Tests;

public class MainViewModelTests
{
    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private readonly CultureInfo _culture = new("de-de");
    private ResourceLoader _resourceLoader = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _resourceLoader = new ResourceLoader();
    }
}