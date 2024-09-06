namespace TimeTracker.Models;

public interface ILocalSettings
{
    string? LastDataFile { get; set; }
}