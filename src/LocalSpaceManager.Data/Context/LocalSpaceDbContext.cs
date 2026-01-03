using LocalSpaceManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalSpaceManager.Data.Context;

/// <summary>
/// Database context for Local Space Manager
/// </summary>
public class LocalSpaceDbContext : DbContext
{
    public DbSet<FileEntity> Files { get; set; }
    
    public LocalSpaceDbContext(DbContextOptions<LocalSpaceDbContext> options)
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure FileEntity
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FullPath).IsUnique();
            entity.HasIndex(e => e.SizeInBytes).IsDescending();
            entity.HasIndex(e => e.ModifiedDate).IsDescending();
            entity.HasIndex(e => e.DirectoryPath);
        });
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LocalSpaceManager",
                "localspace.db");
            
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            
            // Enable WAL mode for better concurrent access
            optionsBuilder.UseSqlite($"Data Source={dbPath}", 
                sqliteOptions => sqliteOptions.CommandTimeout(30));
        }
    }
}
