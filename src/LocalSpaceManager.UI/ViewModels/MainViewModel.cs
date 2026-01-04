using LocalSpaceManager.Core.Interfaces;
using LocalSpaceManager.Core.Models;
using LocalSpaceManager.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace LocalSpaceManager.UI.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IFileRepository _fileRepository;
    private readonly BackgroundScanService _scanService;
    private readonly IRiskEngine _riskEngine;
    
    private bool _isScanning;
    private string _statusMessage = "Ready";
    private string _dbSizeText = "DB Size: 0 B";
    private double _progressValue;
    private string _progressText = string.Empty;
    private string _timeRemainingText = string.Empty;
    
    private string _currentView = "Directories"; // Directories, Files, Cleanup
    private string _currentPath = string.Empty;
    private DirectoryInfoModel? _selectedDirectory;
    
    public ObservableCollection<DirectoryInfoModel> Directories { get; } = new();
    public ObservableCollection<FileInfoModel> Files { get; } = new();
    public ObservableCollection<string> NavigationPath { get; } = new();
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public bool IsScanning { get => _isScanning; set { _isScanning = value; OnPropertyChanged(); } }
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
    public string DbSizeText { get => _dbSizeText; set { _dbSizeText = value; OnPropertyChanged(); } }
    public double ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }
    public string ProgressText { get => _progressText; set { _progressText = value; OnPropertyChanged(); } }
    public string TimeRemainingText { get => _timeRemainingText; set { _timeRemainingText = value; OnPropertyChanged(); } }
    
    public string CurrentView { get => _currentView; set { _currentView = value; OnPropertyChanged(); } }
    public string CurrentPath { get => _currentPath; set { _currentPath = value; OnPropertyChanged(); } }

    public ICommand StartScanCommand { get; }
    public ICommand NavigateToDirectoryCommand { get; }
    public ICommand NavigateBackCommand { get; }
    public ICommand ViewFilesCommand { get; }
    public ICommand ViewCleanupCommand { get; }
    public ICommand ViewTopDirectoriesCommand { get; }

    public MainViewModel(IFileRepository fileRepository, BackgroundScanService scanService, IRiskEngine riskEngine)
    {
        _fileRepository = fileRepository;
        _scanService = scanService;
        _riskEngine = riskEngine;
        
        StartScanCommand = new RelayCommand(async () => await StartScanAsync());
        NavigateToDirectoryCommand = new RelayCommand<DirectoryInfoModel>(async (dir) => await NavigateToDirectoryAsync(dir));
        NavigateBackCommand = new RelayCommand(async () => await NavigateBackAsync());
        ViewFilesCommand = new RelayCommand(async () => await ShowFilesAsync());
        ViewCleanupCommand = new RelayCommand(async () => await ShowCleanupAsync());
        ViewTopDirectoriesCommand = new RelayCommand(async () => await ShowTopDirectoriesAsync());
        
        _scanService.ScanProgressChanged += OnScanProgressChanged;
        _scanService.ScanCompleted += OnScanCompleted;
        
        _ = ShowTopDirectoriesAsync();
    }

    private async Task StartScanAsync()
    {
        IsScanning = true;
        StatusMessage = "Scanning...";
        Directories.Clear();
        Files.Clear();
        
        try
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .Select(d => d.RootDirectory.FullName);
            
            await _scanService.InitialScanAsync(drives);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    public async Task ShowTopDirectoriesAsync()
    {
        CurrentView = "Directories";
        CurrentPath = "Top Occupied Directories";
        NavigationPath.Clear();
        
        Directories.Clear();
        var topDirs = await _fileRepository.GetTopDirectoriesAsync(20);
        foreach (var dir in topDirs) Directories.Add(dir);
        
        UpdateDbSize();
    }

    private async Task NavigateToDirectoryAsync(DirectoryInfoModel? dir)
    {
        if (dir == null) return;
        
        _selectedDirectory = dir;
        CurrentPath = dir.FullPath;
        NavigationPath.Add(dir.DirectoryName);
        
        Directories.Clear();
        var subDirs = await _fileRepository.GetSubDirectoriesAsync(dir.FullPath);
        foreach (var sub in subDirs) Directories.Add(sub);
        
        if (Directories.Count == 0)
        {
            await ShowFilesAsync();
        }
    }

    private async Task NavigateBackAsync()
    {
        if (NavigationPath.Count == 0) return;
        
        NavigationPath.RemoveAt(NavigationPath.Count - 1);
        
        if (NavigationPath.Count == 0)
        {
            await ShowTopDirectoriesAsync();
        }
        else
        {
            var parentPath = Path.GetDirectoryName(CurrentPath);
            if (string.IsNullOrEmpty(parentPath))
            {
                await ShowTopDirectoriesAsync();
            }
            else
            {
                var parentDir = await _fileRepository.GetDirectoryByPathAsync(parentPath);
                await NavigateToDirectoryAsync(parentDir);
                // Remove the extra path added by NavigateToDirectory
                NavigationPath.RemoveAt(NavigationPath.Count - 1);
            }
        }
    }

    private async Task ShowFilesAsync()
    {
        CurrentView = "Files";
        Files.Clear();
        
        var files = await _fileRepository.GetFilesByDirectoryAsync(CurrentPath);
        foreach (var file in files) Files.Add(file);
    }

    private async Task ShowCleanupAsync()
    {
        CurrentView = "Cleanup";
        CurrentPath = "High-Value Cleanup (Large & Old Files)";
        Files.Clear();
        
        var config = _riskEngine.GetConfig();
        var files = await _fileRepository.GetLargeAndOldFilesAsync(config.LargeFileThresholdBytes, config.OldFileThresholdDays);
        foreach (var file in files) Files.Add(file);
    }

    private void OnScanProgressChanged(object? sender, ScanProgress progress)
    {
        ProgressValue = progress.PercentComplete;
        ProgressText = $"{progress.FilesScanned:N0} files found ({FormatBytes(progress.TotalBytesScanned)})";
        TimeRemainingText = progress.RemainingTime.HasValue ? $"Remaining: {FormatTimeSpan(progress.RemainingTime.Value)}" : "";
        StatusMessage = $"Scanning: {progress.CurrentPath}";
    }

    private void OnScanCompleted(object? sender, EventArgs e)
    {
        IsScanning = false;
        StatusMessage = "Scan completed";
        UpdateDbSize();
        _ = ShowTopDirectoriesAsync();
    }

    private void UpdateDbSize()
    {
        try
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalSpaceManager", "localspace.db");
            if (File.Exists(dbPath))
            {
                var info = new FileInfo(dbPath);
                DbSizeText = $"DB Size: {FormatBytes(info.Length)}";
            }
        }
        catch { }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{ts.Minutes}m {ts.Seconds}s";
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    public RelayCommand(Action<T?> execute) => _execute = execute;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute((T?)parameter);
}
