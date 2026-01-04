using LocalSpaceManager.Core.Interfaces;
using LocalSpaceManager.Core.Models;
using LocalSpaceManager.Data.Context;
using LocalSpaceManager.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalSpaceManager.Data.Repositories;

/// <summary>
/// Repository implementation for file metadata operations
/// </summary>
public class FileRepository : IFileRepository
{
    private readonly LocalSpaceDbContext _context;
    
    public FileRepository(LocalSpaceDbContext context)
    {
        _context = context;
    }
    
    public async Task AddFilesAsync(IEnumerable<FileInfoModel> files)
    {
        var entities = files.Select(MapToEntity).ToList();
        await _context.Files.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateFileAsync(FileInfoModel file)
    {
        var entity = await _context.Files
            .FirstOrDefaultAsync(f => f.FullPath == file.FullPath);
            
        if (entity != null)
        {
            entity.FileName = file.FileName;
            entity.DirectoryPath = file.DirectoryPath;
            entity.Extension = file.Extension;
            entity.SizeInBytes = file.SizeInBytes;
            entity.CreatedDate = file.CreatedDate;
            entity.ModifiedDate = file.ModifiedDate;
            entity.LastScannedDate = DateTime.Now;
            entity.RiskLevel = file.RiskLevel;
            entity.RiskExplanation = file.RiskExplanation;
            
            await _context.SaveChangesAsync();
        }
        else
        {
            await _context.Files.AddAsync(MapToEntity(file));
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task DeleteFileAsync(string fullPath)
    {
        var entity = await _context.Files
            .FirstOrDefaultAsync(f => f.FullPath == fullPath);
            
        if (entity != null)
        {
            _context.Files.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<FileInfoModel?> GetFileByPathAsync(string fullPath)
    {
        var entity = await _context.Files
            .FirstOrDefaultAsync(f => f.FullPath == fullPath);
            
        return entity != null ? MapToModel(entity) : null;
    }
    
    public async Task<IEnumerable<FileInfoModel>> GetFilesByDirectoryAsync(string directoryPath)
    {
        var entities = await _context.Files
            .Where(f => f.DirectoryPath == directoryPath)
            .OrderByDescending(f => f.SizeInBytes)
            .ToListAsync();
            
        return entities.Select(MapToModel);
    }
    
    public async Task<IEnumerable<FileInfoModel>> GetLargestFilesAsync(int count)
    {
        var entities = await _context.Files
            .OrderByDescending(f => f.SizeInBytes)
            .Take(count)
            .ToListAsync();
            
        return entities.Select(MapToModel);
    }
    
    public async Task<IEnumerable<FileInfoModel>> GetRecentlyModifiedFilesAsync(int count)
    {
        var entities = await _context.Files
            .OrderByDescending(f => f.ModifiedDate)
            .Take(count)
            .ToListAsync();
            
        return entities.Select(MapToModel);
    }
    
    public async Task<IEnumerable<FileInfoModel>> GetAllFilesOrderedBySizeAsync(int skip, int take)
    {
        var entities = await _context.Files
            .OrderByDescending(f => f.SizeInBytes)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
            
        return entities.Select(MapToModel);
    }
    
    public async Task<IEnumerable<FileInfoModel>> GetAllFilesOrderedByDateAsync(int skip, int take)
    {
        var entities = await _context.Files
            .OrderByDescending(f => f.ModifiedDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
            
        return entities.Select(MapToModel);
    }
    
    public async Task<long> GetTotalFilesCountAsync()
    {
        return await _context.Files.CountAsync();
    }
    
    public async Task<long> GetTotalSizeAsync()
    {
        return await _context.Files.SumAsync(f => f.SizeInBytes);
    }
    
    public async Task ClearAllAsync()
    {
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Files");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Directories");
        await _context.Database.ExecuteSqlRawAsync("VACUUM");
    }

    public async Task AddDirectoriesAsync(IEnumerable<DirectoryInfoModel> directories)
    {
        var entities = directories.Select(MapToDirectoryEntity).ToList();
        await _context.Directories.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<DirectoryInfoModel>> GetTopDirectoriesAsync(int count, string? driveRoot = null)
    {
        var query = _context.Directories.AsQueryable();
        if (!string.IsNullOrEmpty(driveRoot))
        {
            query = query.Where(d => d.FullPath.StartsWith(driveRoot));
        }

        var entities = await query
            .OrderByDescending(d => d.TotalSizeInBytes)
            .Take(count)
            .ToListAsync();
        return entities.Select(MapToDirectoryModel);
    }

    public async Task<IEnumerable<DirectoryInfoModel>> GetSubDirectoriesAsync(string parentPath)
    {
        var entities = await _context.Directories
            .Where(d => d.ParentPath == parentPath)
            .OrderByDescending(d => d.TotalSizeInBytes)
            .ToListAsync();
        return entities.Select(MapToDirectoryModel);
    }

    public async Task<DirectoryInfoModel?> GetDirectoryByPathAsync(string path)
    {
        var entity = await _context.Directories
            .FirstOrDefaultAsync(d => d.FullPath == path);
        return entity != null ? MapToDirectoryModel(entity) : null;
    }

    public async Task AggregateDirectoryDataAsync()
    {
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<FileInfoModel>> GetLargeAndOldFilesAsync(long minSizeInBytes, int minAgeInDays)
    {
        var cutoffDate = DateTime.Now.AddDays(-minAgeInDays);
        var entities = await _context.Files
            .Where(f => f.SizeInBytes >= minSizeInBytes && f.ModifiedDate <= cutoffDate)
            .OrderByDescending(f => f.SizeInBytes)
            .ToListAsync();
        return entities.Select(MapToModel);
    }
    
    private static DirectoryEntity MapToDirectoryEntity(DirectoryInfoModel model)
    {
        return new DirectoryEntity
        {
            Id = model.Id,
            FullPath = model.FullPath,
            DirectoryName = model.DirectoryName,
            ParentPath = model.ParentPath,
            TotalSizeInBytes = model.TotalSizeInBytes,
            FileCount = model.FileCount,
            LastModifiedDate = model.LastModifiedDate,
            RiskLevel = model.RiskLevel,
            RiskExplanation = model.RiskExplanation,
            MainFileTypes = model.MainFileTypes
        };
    }

    private static DirectoryInfoModel MapToDirectoryModel(DirectoryEntity entity)
    {
        return new DirectoryInfoModel
        {
            Id = entity.Id,
            FullPath = entity.FullPath,
            DirectoryName = entity.DirectoryName,
            ParentPath = entity.ParentPath,
            TotalSizeInBytes = entity.TotalSizeInBytes,
            FileCount = entity.FileCount,
            LastModifiedDate = entity.LastModifiedDate,
            RiskLevel = entity.RiskLevel,
            RiskExplanation = entity.RiskExplanation,
            MainFileTypes = entity.MainFileTypes
        };
    }

    private static FileEntity MapToEntity(FileInfoModel model)
    {
        return new FileEntity
        {
            Id = model.Id,
            FullPath = model.FullPath,
            FileName = model.FileName,
            DirectoryPath = model.DirectoryPath,
            Extension = model.Extension,
            SizeInBytes = model.SizeInBytes,
            CreatedDate = model.CreatedDate,
            ModifiedDate = model.ModifiedDate,
            LastScannedDate = model.LastScannedDate,
            RiskLevel = model.RiskLevel,
            RiskExplanation = model.RiskExplanation
        };
    }
    
    private static FileInfoModel MapToModel(FileEntity entity)
    {
        return new FileInfoModel
        {
            Id = entity.Id,
            FullPath = entity.FullPath,
            FileName = entity.FileName,
            DirectoryPath = entity.DirectoryPath,
            Extension = entity.Extension,
            SizeInBytes = entity.SizeInBytes,
            CreatedDate = entity.CreatedDate,
            ModifiedDate = entity.ModifiedDate,
            LastScannedDate = entity.LastScannedDate,
            RiskLevel = entity.RiskLevel,
            RiskExplanation = entity.RiskExplanation
        };
    }
}
