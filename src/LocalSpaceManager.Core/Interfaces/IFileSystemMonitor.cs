namespace LocalSpaceManager.Core.Interfaces;

/// <summary>
/// Interface for monitoring file system changes
/// </summary>
public interface IFileSystemMonitor
{
    event EventHandler<string>? FileCreated;
    event EventHandler<string>? FileModified;
    event EventHandler<string>? FileDeleted;
    event EventHandler<string>? FileRenamed;
    
    void StartMonitoring(IEnumerable<string> paths);
    void StopMonitoring();
    bool IsMonitoring { get; }
}
