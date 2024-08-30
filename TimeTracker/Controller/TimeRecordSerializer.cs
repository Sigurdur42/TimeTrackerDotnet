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

    public TimeRecord[] Deserialize(string csvContent)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        using var reader = new StreamReader(stream);

        using var csv = new CsvReader(reader, _configuration);
        csv.Context.TypeConverterCache.AddConverter<bool>(new PythonBooleanConverter());
        return csv.GetRecords<TimeRecord>().ToArray();
    }
}