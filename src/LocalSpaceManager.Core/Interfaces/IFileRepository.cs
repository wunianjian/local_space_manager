using LocalSpaceManager.Core.Models;

namespace LocalSpaceManager.Core.Interfaces;

/// <summary>
/// Interface for file metadata repository operations
/// </summary>
public interface IFileRepository
{
    Task AddFilesAsync(IEnumerable<FileInfoModel> files);
    Task UpdateFileAsync(FileInfoModel file);
    Task DeleteFileAsync(string fullPath);
    Task<FileInfoModel?> GetFileByPathAsync(string fullPath);
    Task<IEnumerable<FileInfoModel>> GetFilesByDirectoryAsync(string directoryPath);
    Task<IEnumerable<FileInfoModel>> GetLargestFilesAsync(int count);
    Task<IEnumerable<FileInfoModel>> GetRecentlyModifiedFilesAsync(int count);
    Task<IEnumerable<FileInfoModel>> GetAllFilesOrderedBySizeAsync(int skip, int take);
    Task<IEnumerable<FileInfoModel>> GetAllFilesOrderedByDateAsync(int skip, int take);
    Task<long> GetTotalFilesCountAsync();
    Task<long> GetTotalSizeAsync();
    Task ClearAllAsync();

    // Directory operations
    Task AddDirectoriesAsync(IEnumerable<DirectoryInfoModel> directories);
    Task<IEnumerable<DirectoryInfoModel>> GetTopDirectoriesAsync(int count, string? driveRoot = null);
    Task<IEnumerable<DirectoryInfoModel>> GetSubDirectoriesAsync(string parentPath);
    Task<DirectoryInfoModel?> GetDirectoryByPathAsync(string path);
    Task AggregateDirectoryDataAsync();
    
    // Cleanup views
    Task<IEnumerable<FileInfoModel>> GetLargeAndOldFilesAsync(long minSizeInBytes, int minAgeInDays);
}
