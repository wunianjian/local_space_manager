using LocalSpaceManager.Core.Interfaces;
using LocalSpaceManager.Core.Models;
using Microsoft.Extensions.Logging;

namespace LocalSpaceManager.Core.Services;

/// <summary>
/// Background service that coordinates file scanning and monitoring
/// </summary>
public class BackgroundScanService : IDisposable
{
    private readonly IFileScanner _fileScanner;
    private readonly IFileSystemMonitor _fileSystemMonitor;
    private readonly IFileRepository _fileRepository;
    private readonly ILogger<BackgroundScanService>? _logger;
    private readonly SemaphoreSlim _updateSemaphore = new(1, 1);
    
    public event EventHandler<ScanProgress>? ScanProgressChanged;
    public event EventHandler? ScanCompleted;
    
    public BackgroundScanService(
        IFileScanner fileScanner,
        IFileSystemMonitor fileSystemMonitor,
        IFileRepository fileRepository,
        ILogger<BackgroundScanService>? logger = null)
    {
        _fileScanner = fileScanner;
        _fileSystemMonitor = fileSystemMonitor;
        _fileRepository = fileRepository;
        _logger = logger;
        
        // Subscribe to file system events
        _fileSystemMonitor.FileCreated += OnFileCreated;
        _fileSystemMonitor.FileModified += OnFileModified;
        _fileSystemMonitor.FileDeleted += OnFileDeleted;
        _fileSystemMonitor.FileRenamed += OnFileRenamed;
    }
    
    /// <summary>
    /// Performs initial scan of specified paths
    /// </summary>
    public async Task InitialScanAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Starting initial scan of {Count} paths", paths.Count());
        
        var progress = new Progress<ScanProgress>(p =>
        {
            ScanProgressChanged?.Invoke(this, p);
        });
        
        try
        {
            // Clear existing data
            await _fileRepository.ClearAllAsync();
            
            // Scan all paths
            var files = await _fileScanner.ScanPathsAsync(paths, progress, cancellationToken);
            
            // Save to database in batches
            var fileList = files.ToList();
            var batchSize = 2000; // Increased batch size for faster DB operations
            
            // Run DB operations in a separate task to keep UI responsive
            await Task.Run(async () => 
            {
                for (int i = 0; i < fileList.Count; i += batchSize)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    var batch = fileList.Skip(i).Take(batchSize);
                    await _fileRepository.AddFilesAsync(batch);
                    
                    // Report DB saving progress
                    var dbProgress = new ScanProgress
                    {
                        FilesScanned = fileList.Count,
                        CurrentPath = $"Saving to database: {i:N0} / {fileList.Count:N0}",
                        PercentComplete = 99, // Almost done
                        IsComplete = false
                    };
                    ScanProgressChanged?.Invoke(this, dbProgress);
                }
            }, cancellationToken);
            
            _logger?.LogInformation("Initial scan completed. Total files: {Count}", fileList.Count);
            ScanCompleted?.Invoke(this, EventArgs.Empty);
            
            // Start monitoring after initial scan
            _fileSystemMonitor.StartMonitoring(paths);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during initial scan");
            throw;
        }
    }
    
    /// <summary>
    /// Starts monitoring file system changes
    /// </summary>
    public void StartMonitoring(IEnumerable<string> paths)
    {
        _fileSystemMonitor.StartMonitoring(paths);
        _logger?.LogInformation("File system monitoring started");
    }
    
    /// <summary>
    /// Stops monitoring file system changes
    /// </summary>
    public void StopMonitoring()
    {
        _fileSystemMonitor.StopMonitoring();
        _logger?.LogInformation("File system monitoring stopped");
    }
    
    private async void OnFileCreated(object? sender, string filePath)
    {
        await UpdateFileInDatabaseAsync(filePath);
    }
    
    private async void OnFileModified(object? sender, string filePath)
    {
        await UpdateFileInDatabaseAsync(filePath);
    }
    
    private async void OnFileDeleted(object? sender, string filePath)
    {
        await _updateSemaphore.WaitAsync();
        try
        {
            await _fileRepository.DeleteFileAsync(filePath);
            _logger?.LogDebug("Removed file from database: {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting file from database: {Path}", filePath);
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }
    
    private async void OnFileRenamed(object? sender, string filePath)
    {
        await UpdateFileInDatabaseAsync(filePath);
    }
    
    private async Task UpdateFileInDatabaseAsync(string filePath)
    {
        await _updateSemaphore.WaitAsync();
        try
        {
            // Wait a bit to ensure file is fully written
            await Task.Delay(100);
            
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                var fileModel = new FileInfoModel
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
                
                await _fileRepository.UpdateFileAsync(fileModel);
                _logger?.LogDebug("Updated file in database: {Path}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating file in database: {Path}", filePath);
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }
    
    public void Dispose()
    {
        _fileSystemMonitor.StopMonitoring();
        _updateSemaphore.Dispose();
    }
}
