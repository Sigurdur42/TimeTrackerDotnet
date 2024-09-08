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
    private readonly CsvConfiguration _configuration = new(new CultureInfo("de-de"))
    {
        NewLine = Environment.NewLine,
        Encoding = Encoding.UTF8,
        HasHeaderRecord = false,
    };

    public TimeRecord[] Deserialize(FileInfo file)
    {
        return Deserialize(File.ReadAllText(file.FullName));
    }

    public TimeRecord[] Deserialize(string csvContent)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        using var reader = new StreamReader(stream);

        using var csv = new CsvReader(reader, _configuration);
        csv.Context.TypeConverterCache.AddConverter<bool>(new PythonBooleanConverter());
        csv.Context.TypeConverterCache.AddConverter<TimeOnly>(new TimeOnlyConverter()); 
       return csv.GetRecords<TimeRecord>().ToArray();
    }

    public string Serialize(TimeRecord[] records)
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, _configuration);
        csv.Context.TypeConverterCache.AddConverter<bool>(new PythonBooleanConverter());
        csv.Context.TypeConverterCache.AddConverter<TimeOnly>(new TimeOnlyConverter()); 
        csv.WriteRecords(records);
        return writer.ToString();
    }

    public void Serialize(FileInfo file, TimeRecord[] records)
    {
        var content = Serialize(records);
        File.WriteAllText(file.FullName, content, Encoding.UTF8);
    }
}