namespace LocalSpaceManager.Core.Models;

public class RiskRule
{
    public string Pattern { get; set; } = string.Empty;
    public RiskLevel Level { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public bool IsExtension { get; set; } // True if pattern is an extension, false if it's a path fragment
}

public class RiskConfig
{
    public List<RiskRule> Rules { get; set; } = new();
    public long LargeFileThresholdBytes { get; set; } = 500 * 1024 * 1024; // 500MB
    public int OldFileThresholdDays { get; set; } = 180; // 180 days
}
