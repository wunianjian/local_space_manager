namespace LocalSpaceManager.Core.Models;

/// <summary>
/// Represents metadata information for a file in the system
/// </summary>
public class FileInfoModel
{
    public long Id { get; set; }
    
    public string FullPath { get; set; } = string.Empty;
    
    public string FileName { get; set; } = string.Empty;
    
    public string DirectoryPath { get; set; } = string.Empty;
    
    public string Extension { get; set; } = string.Empty;
    
    public long SizeInBytes { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime ModifiedDate { get; set; }
    
    public DateTime LastScannedDate { get; set; }
    
    public RiskLevel RiskLevel { get; set; }
    
    public string RiskExplanation { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets the human-readable file size
    /// </summary>
    public string FormattedSize => FormatBytes(SizeInBytes);
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
}
