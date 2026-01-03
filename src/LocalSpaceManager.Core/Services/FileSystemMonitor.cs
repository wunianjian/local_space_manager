using LocalSpaceManager.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LocalSpaceManager.Core.Services;

/// <summary>
/// Service for monitoring file system changes in real-time
/// </summary>
public class FileSystemMonitor : IFileSystemMonitor, IDisposable
{
    private readonly ILogger<FileSystemMonitor>? _logger;
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly ConcurrentDictionary<string, DateTime> _recentChanges = new();
    private readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(500);
    private bool _isMonitoring;
    
    public event EventHandler<string>? FileCreated;
    public event EventHandler<string>? FileModified;
    public event EventHandler<string>? FileDeleted;
    public event EventHandler<string>? FileRenamed;
    
    public bool IsMonitoring => _isMonitoring;
    
    public FileSystemMonitor(ILogger<FileSystemMonitor>? logger = null)
    {
        _logger = logger;
    }
    
    public void StartMonitoring(IEnumerable<string> paths)
    {
        if (_isMonitoring)
        {
            _logger?.LogWarning("File system monitoring is already active");
            return;
        }
        
        StopMonitoring();
        
        foreach (var path in paths)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    var watcher = CreateWatcher(path);
                    _watchers.Add(watcher);
                    watcher.EnableRaisingEvents = true;
                    _logger?.LogInformation("Started monitoring path: {Path}", path);
                }
                else
                {
                    _logger?.LogWarning("Path does not exist: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error starting monitoring for path: {Path}", path);
            }
        }
        
        _isMonitoring = true;
    }
    
    public void StopMonitoring()
    {
        foreach (var watcher in _watchers)
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping file watcher");
            }
        }
        
        _watchers.Clear();
        _recentChanges.Clear();
        _isMonitoring = false;
        _logger?.LogInformation("Stopped file system monitoring");
    }
    
    private FileSystemWatcher CreateWatcher(string path)
    {
        var watcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName 
                         | NotifyFilters.DirectoryName 
                         | NotifyFilters.Size 
                         | NotifyFilters.LastWrite,
            IncludeSubdirectories = true,
            InternalBufferSize = 64 * 1024 // 64KB buffer
        };
        
        watcher.Created += OnFileCreated;
        watcher.Changed += OnFileChanged;
        watcher.Deleted += OnFileDeleted;
        watcher.Renamed += OnFileRenamed;
        watcher.Error += OnError;
        
        return watcher;
    }
    
    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (ShouldProcessEvent(e.FullPath))
        {
            _logger?.LogDebug("File created: {Path}", e.FullPath);
            FileCreated?.Invoke(this, e.FullPath);
        }
    }
    
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (ShouldProcessEvent(e.FullPath))
        {
            _logger?.LogDebug("File modified: {Path}", e.FullPath);
            FileModified?.Invoke(this, e.FullPath);
        }
    }
    
    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (ShouldProcessEvent(e.FullPath))
        {
            _logger?.LogDebug("File deleted: {Path}", e.FullPath);
            FileDeleted?.Invoke(this, e.FullPath);
        }
    }
    
    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (ShouldProcessEvent(e.FullPath))
        {
            _logger?.LogDebug("File renamed: {OldPath} -> {NewPath}", e.OldFullPath, e.FullPath);
            
            // Treat rename as delete old + create new
            FileDeleted?.Invoke(this, e.OldFullPath);
            FileRenamed?.Invoke(this, e.FullPath);
        }
    }
    
    private void OnError(object sender, ErrorEventArgs e)
    {
        var exception = e.GetException();
        _logger?.LogError(exception, "File system watcher error");
    }
    
    /// <summary>
    /// Debounces events to prevent processing the same file multiple times in quick succession
    /// </summary>
    private bool ShouldProcessEvent(string filePath)
    {
        var now = DateTime.Now;
        
        // Check if we've recently processed this file
        if (_recentChanges.TryGetValue(filePath, out var lastProcessed))
        {
            if (now - lastProcessed < _debounceInterval)
            {
                return false; // Skip this event
            }
        }
        
        // Update or add the timestamp
        _recentChanges[filePath] = now;
        
        // Clean up old entries (older than 5 seconds)
        var cutoff = now - TimeSpan.FromSeconds(5);
        var keysToRemove = _recentChanges
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();
            
        foreach (var key in keysToRemove)
        {
            _recentChanges.TryRemove(key, out _);
        }
        
        return true;
    }
    
    public void Dispose()
    {
        StopMonitoring();
    }
}
