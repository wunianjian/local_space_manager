using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalSpaceManager.Data.Entities;

/// <summary>
/// Database entity representing a file's metadata
/// </summary>
[Table("Files")]
[Index(nameof(SizeInBytes), IsDescending = new[] { true })]
[Index(nameof(ModifiedDate), IsDescending = new[] { true })]
[Index(nameof(FullPath), IsUnique = true)]
public class FileEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    
    [Required]
    [MaxLength(2048)]
    public string FullPath { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(2048)]
    public string DirectoryPath { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Extension { get; set; } = string.Empty;
    
    public long SizeInBytes { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime ModifiedDate { get; set; }
    
    public DateTime LastScannedDate { get; set; }
}
