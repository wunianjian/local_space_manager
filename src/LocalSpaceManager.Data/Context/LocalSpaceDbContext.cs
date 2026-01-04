using LocalSpaceManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalSpaceManager.Data.Context;

/// <summary>
/// Database context for Local Space Manager
/// </summary>
public class LocalSpaceDbContext : DbContext
{
    public DbSet<FileEntity> Files { get; set; }
    public DbSet<DirectoryEntity> Directories { get; set; }
    
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

        // Configure DirectoryEntity
        modelBuilder.Entity<DirectoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FullPath).IsUnique();
            entity.HasIndex(e => e.ParentPath);
            entity.HasIndex(e => e.TotalSizeInBytes).IsDescending();
        });
    }
}
