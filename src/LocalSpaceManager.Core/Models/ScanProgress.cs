namespace LocalSpaceManager.Core.Models;

/// <summary>
/// Represents the progress of a file system scan operation
/// </summary>
public class ScanProgress
{
    public int FilesScanned { get; set; }
    
    public long TotalBytesScanned { get; set; }
    
    public string CurrentPath { get; set; } = string.Empty;
    
    public int PercentComplete { get; set; }
    
    public TimeSpan ElapsedTime { get; set; }
    
    public bool IsComplete { get; set; }
    
    public string? ErrorMessage { get; set; }
}
