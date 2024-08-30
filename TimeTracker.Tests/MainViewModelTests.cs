using System.Globalization;
using System.Reflection;

namespace TimeTracker.Tests;

public class MainViewModelTests
{
    ResourceLoader _resourceLoader = null!;
    readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    readonly CultureInfo _culture = new ("de-de");

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _resourceLoader = new ResourceLoader();
    }
}