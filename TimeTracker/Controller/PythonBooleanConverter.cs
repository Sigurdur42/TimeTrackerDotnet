using CsvHelper;

namespace TimeTracker.Controller;

/// <summary>
///     Reads an empty field as <c>false</c> and writes <c>false</c> as an empty field (format of the original Python tool).
/// </summary>
public sealed class PythonBooleanConverter : ICsvConverter<bool>
{
    public bool Read(CsvReader reader, int index)
    {
        return !reader.IsEmpty(index) && reader.Parse<bool>(index);
    }

    public void Write(CsvWriter writer, bool value)
    {
        writer.WriteField(value ? "True" : "");
    }
}
