using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using TimeTracker.Models;

namespace TimeTracker.Controller;

public class TimeRecordSerializer
{
    private static readonly CultureInfo Culture = new("de-de");

    private static readonly CsvReaderOptions ReaderOptions = CsvReaderOptions.Default with
    {
        Separator = ';',
        HasHeader = false,
        FormatProvider = Culture
    };

    private static readonly CsvWriterOptions WriterOptions = CsvWriterOptions.Default with
    {
        Separator = ';',
        HasHeader = false,
        NewLine = "\n",
        // quote values with leading/trailing whitespace as CsvHelper 33 did, so existing files stay unchanged
        Quoting = CsvQuoting.WhenNeededOrWhitespace,
        FormatProvider = Culture
    };

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
        using var csv = CsvReader.Create(reader, ReaderOptions, true);
        return csv.GetRecords<TimeRecord>().ToArray();
    }

    public string Serialize(TimeRecord[] records)
    {
        using var writer = new StringWriter();
        using (var csv = CsvWriter.Create(writer, WriterOptions, true))
        {
            csv.WriteRecords(records);
        }

        return writer.ToString();
    }

    public void Serialize(FileInfo file, TimeRecord[] records)
    {
        var content = Serialize(records);
        File.WriteAllText(file.FullName, content, Encoding.UTF8);
    }
}
