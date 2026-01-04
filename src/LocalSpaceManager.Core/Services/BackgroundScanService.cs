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
    private readonly IRiskEngine _riskEngine;
    private readonly ILogger<BackgroundScanService>? _logger;
    private readonly SemaphoreSlim _updateSemaphore = new(1, 1);
    
    public event EventHandler<ScanProgress>? ScanProgressChanged;
    public event EventHandler? ScanCompleted;
    
    public BackgroundScanService(
        IFileScanner fileScanner,
        IFileSystemMonitor fileSystemMonitor,
        IFileRepository fileRepository,
        IRiskEngine riskEngine,
        ILogger<BackgroundScanService>? logger = null)
    {
        _fileScanner = fileScanner;
        _fileSystemMonitor = fileSystemMonitor;
        _fileRepository = fileRepository;
        _riskEngine = riskEngine;
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
            var batchSize = 2000; 
            
            await Task.Run(async () => 
            {
                for (int i = 0; i < fileList.Count; i += batchSize)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                        
                    var batch = fileList.Skip(i).Take(batchSize).ToList();
                    
                    foreach (var file in batch)
                    {
                        var risk = _riskEngine.GetRisk(file.FullPath, false);
                        file.RiskLevel = risk.Level;
                        file.RiskExplanation = risk.Explanation;
                    }

                    await _fileRepository.AddFilesAsync(batch);
                    
                    var dbProgress = new ScanProgress
                    {
                        FilesScanned = fileList.Count,
                        CurrentPath = $"Saving to database: {i:N0} / {fileList.Count:N0}",
                        PercentComplete = 99, 
                        IsComplete = false
                    };
                    ScanProgressChanged?.Invoke(this, dbProgress);
                }

                // Aggregate directory data
                var aggProgress = new ScanProgress
                {
                    FilesScanned = fileList.Count,
                    CurrentPath = "Analyzing directory structures...",
                    PercentComplete = 99.5,
                    IsComplete = false
                };
                ScanProgressChanged?.Invoke(this, aggProgress);
                
                await AggregateDirectoriesAsync(fileList, cancellationToken);
                
                var finalProgress = new ScanProgress
                {
                    FilesScanned = fileList.Count,
                    CurrentPath = "Scan Complete",
                    PercentComplete = 100,
                    IsComplete = true
                };
                ScanProgressChanged?.Invoke(this, finalProgress);
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
    
    private async Task AggregateDirectoriesAsync(List<FileInfoModel> files, CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Aggregating directory data...");
        
        var dirMap = new Dictionary<string, DirectoryInfoModel>();
        
        foreach (var file in files)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            var path = file.DirectoryPath;
            while (!string.IsNullOrEmpty(path))
            {
                if (!dirMap.TryGetValue(path, out var dirInfo))
                {
                    var risk = _riskEngine.GetRisk(path, true);
                    dirInfo = new DirectoryInfoModel
                    {
                        FullPath = path,
                        DirectoryName = Path.GetFileName(path),
                        ParentPath = Path.GetDirectoryName(path) ?? string.Empty,
                        RiskLevel = risk.Level,
                        RiskExplanation = risk.Explanation
                    };
                    dirMap[path] = dirInfo;
                }
                
                dirInfo.TotalSizeInBytes += file.SizeInBytes;
                dirInfo.FileCount++;
                if (file.ModifiedDate > dirInfo.LastModifiedDate)
                    dirInfo.LastModifiedDate = file.ModifiedDate;
                
                path = Path.GetDirectoryName(path);
            }
        }
        
        _logger?.LogInformation("Saving {Count} directories to database...", dirMap.Count);
        
        var dirList = dirMap.Values.ToList();
        var batchSize = 1000;
        for (int i = 0; i < dirList.Count; i += batchSize)
        {
            await _fileRepository.AddDirectoriesAsync(dirList.Skip(i).Take(batchSize));
        }
    }

    private async Task UpdateFileInDatabaseAsync(string filePath)
    {
        await _updateSemaphore.WaitAsync();
        try
        {
            await Task.Delay(100);
            
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                var risk = _riskEngine.GetRisk(fileInfo.FullName, false);
                var fileModel = new FileInfoModel
                {
                    FullPath = fileInfo.FullName,
                    FileName = fileInfo.Name,
                    DirectoryPath = fileInfo.DirectoryName ?? string.Empty,
                    Extension = fileInfo.Extension,
                    SizeInBytes = fileInfo.Length,
                    CreatedDate = fileInfo.CreationTime,
                    ModifiedDate = fileInfo.LastWriteTime,
                    LastScannedDate = DateTime.Now,
                    RiskLevel = risk.Level,
                    RiskExplanation = risk.Explanation
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
