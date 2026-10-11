using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using TimeTracker.Models;

namespace TimeTracker.Controller;

public class TimeRecordSerializer
{
    private static readonly CsvConfiguration Configuration = new(new CultureInfo("de-de"))
    {
        NewLine = "\n",
        Encoding = Encoding.UTF8,
        HasHeaderRecord = false
    };

    private static readonly PythonBooleanConverter BooleanConverter = new();
    private static readonly TimeOnlyConverter TimeConverter = new();

    public TimeRecord[] Deserialize(FileInfo file)
    {
        using var reader = new StreamReader(file.FullName, Encoding.UTF8);
        return Deserialize(reader);
    }

    public TimeRecord[] Deserialize(string csvContent)
    {
        using var reader = new StringReader(csvContent);
        return Deserialize(reader);
    }

    private static TimeRecord[] Deserialize(TextReader reader)
    {
        using var csv = new CsvReader(reader, Configuration);
        csv.Context.TypeConverterCache.AddConverter<bool>(BooleanConverter);
        csv.Context.TypeConverterCache.AddConverter<TimeOnly>(TimeConverter);
        return csv.GetRecords<TimeRecord>().ToArray();
    }

    public string Serialize(TimeRecord[] records)
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, Configuration);
        csv.Context.TypeConverterCache.AddConverter<bool>(BooleanConverter);
        csv.Context.TypeConverterCache.AddConverter<TimeOnly>(TimeConverter);
        csv.WriteRecords(records);
        return writer.ToString();
    }

    public void Serialize(FileInfo file, TimeRecord[] records)
    {
        var content = Serialize(records);
        File.WriteAllText(file.FullName, content, Encoding.UTF8);
    }
}
