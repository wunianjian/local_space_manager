using LocalSpaceManager.Core.Models;

namespace LocalSpaceManager.Core.Interfaces;

/// <summary>
/// Interface for scanning file system and collecting file metadata
/// </summary>
public interface IFileScanner
{
    /// <summary>
    /// Scans the specified paths and returns file information
    /// </summary>
    Task<IEnumerable<FileInfoModel>> ScanPathsAsync(
        IEnumerable<string> paths, 
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Scans a single path
    /// </summary>
    Task<IEnumerable<FileInfoModel>> ScanPathAsync(
        string path, 
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
