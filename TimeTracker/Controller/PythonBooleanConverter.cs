using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace TimeTracker.Controller;

public class PythonBooleanConverter : BooleanConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (text is null || text.Length == 0)
        {
            text = "false";
        }
        else if (text == "True")
        {
            text = "true";
        }

        return base.ConvertFromString(text, row, memberMapData);
    }
}