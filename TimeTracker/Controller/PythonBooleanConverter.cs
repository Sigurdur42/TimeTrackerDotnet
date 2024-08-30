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

        return base.ConvertFromString(text, row, memberMapData);
    }

    public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        var result = base.ConvertToString(value, row, memberMapData);
        result = result switch
        {
            "False" => "",
            _ => result
        };

        return result;
    }
}