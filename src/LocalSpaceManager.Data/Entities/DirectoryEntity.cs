using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using LocalSpaceManager.Core.Models;

namespace LocalSpaceManager.Data.Entities;

[Index(nameof(FullPath), IsUnique = true)]
[Index(nameof(ParentPath))]
[Index(nameof(TotalSizeInBytes), IsDescending = true)]
public class DirectoryEntity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string FullPath { get; set; } = string.Empty;
    
    [Required]
    public string DirectoryName { get; set; } = string.Empty;
    
    public string ParentPath { get; set; } = string.Empty;
    
    public long TotalSizeInBytes { get; set; }
    
    public int FileCount { get; set; }
    
    public DateTime LastModifiedDate { get; set; }
    
    public RiskLevel RiskLevel { get; set; }
    
    public string RiskExplanation { get; set; } = string.Empty;
    
    public string MainFileTypes { get; set; } = string.Empty;
}
