namespace LocalSpaceManager.Core.Models;

public enum RiskLevel
{
    Safe,
    Review,
    HighRisk
}

public class DirectoryInfoModel
{
    public int Id { get; set; }
    public string FullPath { get; set; } = string.Empty;
    public string DirectoryName { get; set; } = string.Empty;
    public string ParentPath { get; set; } = string.Empty;
    public long TotalSizeInBytes { get; set; }
    public int FileCount { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string RiskExplanation { get; set; } = string.Empty;
    public string MainFileTypes { get; set; } = string.Empty;
}
