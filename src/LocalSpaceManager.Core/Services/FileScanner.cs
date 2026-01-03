using LocalSpaceManager.Core.Interfaces;
using LocalSpaceManager.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LocalSpaceManager.Core.Services;

/// <summary>
/// Service for scanning file system and collecting file metadata
/// </summary>
public class FileScanner : IFileScanner
{
    private readonly ILogger<FileScanner>? _logger;
    
    public FileScanner(ILogger<FileScanner>? logger = null)
    {
        _logger = logger;
    }
    
    public async Task<IEnumerable<FileInfoModel>> ScanPathAsync(
        string path, 
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await ScanPathsAsync(new[] { path }, progress, cancellationToken);
    }
    
    public async Task<IEnumerable<FileInfoModel>> ScanPathsAsync(
        IEnumerable<string> paths, 
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var files = new List<FileInfoModel>();
        var stopwatch = Stopwatch.StartNew();
        var filesScanned = 0;
        long totalBytes = 0;
        
        // Estimate total files for progress bar (rough estimate based on drive stats if possible)
        // For now, we'll use a dynamic estimation that grows as we find more files
        int estimatedTotalFiles = 100000; 

        foreach (var path in paths)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            try
            {
                if (Directory.Exists(path))
                {
                    await Task.Run(() => ScanDirectory(
                        path, 
                        files, 
                        ref filesScanned, 
                        ref totalBytes, 
                        progress, 
                        stopwatch, 
                        cancellationToken), cancellationToken);
                }
                else if (File.Exists(path))
                {
                    var fileInfo = CreateFileInfoModel(path);
                    if (fileInfo != null)
                    {
                        files.Add(fileInfo);
                        filesScanned++;
                        totalBytes += fileInfo.SizeInBytes;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error scanning path: {Path}", path);
                progress?.Report(new ScanProgress
                {
                    FilesScanned = filesScanned,
                    TotalBytesScanned = totalBytes,
                    CurrentPath = path,
                    ElapsedTime = stopwatch.Elapsed,
                    ErrorMessage = ex.Message
                });
            }
        }
        
        progress?.Report(new ScanProgress
        {
            FilesScanned = filesScanned,
            TotalBytesScanned = totalBytes,
            CurrentPath = string.Empty,
            PercentComplete = 100,
            ElapsedTime = stopwatch.Elapsed,
            IsComplete = true
        });
        
        return files;
    }
    
    private void ScanDirectory(
        string directoryPath,
        List<FileInfoModel> files,
        ref int filesScanned,
        ref long totalBytes,
        IProgress<ScanProgress>? progress,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        try
        {
            // Scan files in current directory
            var dirInfo = new DirectoryInfo(directoryPath);
            
            foreach (var file in dirInfo.EnumerateFiles())
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                    
                try
                {
                    var fileModel = CreateFileInfoModel(file);
                    if (fileModel != null)
                    {
                        files.Add(fileModel);
                        filesScanned++;
                        totalBytes += fileModel.SizeInBytes;
                        
                        // Report progress every 500 files for better performance
                        if (filesScanned % 500 == 0)
                        {
                            // Adjust estimation if we exceed it
                            if (filesScanned >= estimatedTotalFiles)
                                estimatedTotalFiles = (int)(filesScanned * 1.5);

                            var elapsed = stopwatch.Elapsed;
                            var speed = filesScanned / elapsed.TotalSeconds;
                            var remainingFiles = estimatedTotalFiles - filesScanned;
                            var remainingTime = speed > 0 ? TimeSpan.FromSeconds(remainingFiles / speed) : TimeSpan.Zero;

                            progress?.Report(new ScanProgress
                            {
                                FilesScanned = filesScanned,
                                TotalBytesScanned = totalBytes,
                                CurrentPath = file.FullName,
                                ElapsedTime = elapsed,
                                PercentComplete = Math.Min(99, (double)filesScanned / estimatedTotalFiles * 100),
                                ScanSpeed = speed,
                                RemainingTime = remainingTime,
                                TotalFilesEstimated = estimatedTotalFiles
                            });
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip files we don't have access to
                    _logger?.LogDebug("Access denied to file: {Path}", file.FullName);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error processing file: {Path}", file.FullName);
                }
            }
            
            // Recursively scan subdirectories
            foreach (var subDir in dirInfo.EnumerateDirectories())
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                    
                try
                {
                    ScanDirectory(subDir.FullName, files, ref filesScanned, ref totalBytes, progress, stopwatch, cancellationToken);
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip directories we don't have access to
                    _logger?.LogDebug("Access denied to directory: {Path}", subDir.FullName);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error scanning directory: {Path}", subDir.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error accessing directory: {Path}", directoryPath);
        }
    }
    
    private FileInfoModel? CreateFileInfoModel(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            return CreateFileInfoModel(fileInfo);
        }
        catch
        {
            return null;
        }
    }
    
    private FileInfoModel? CreateFileInfoModel(FileInfo fileInfo)
    {
        try
        {
            return new FileInfoModel
            {
                FullPath = fileInfo.FullName,
                FileName = fileInfo.Name,
                DirectoryPath = fileInfo.DirectoryName ?? string.Empty,
                Extension = fileInfo.Extension,
                SizeInBytes = fileInfo.Length,
                CreatedDate = fileInfo.CreationTime,
                ModifiedDate = fileInfo.LastWriteTime,
                LastScannedDate = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error creating file model for: {Path}", fileInfo.FullName);
            return null;
        }
    }
}
