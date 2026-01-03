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
        
        // Use batch insert for better performance
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
            
            await _context.SaveChangesAsync();
        }
        else
        {
            // File doesn't exist, add it
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
            LastScannedDate = model.LastScannedDate
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
            LastScannedDate = entity.LastScannedDate
        };
    }
}
