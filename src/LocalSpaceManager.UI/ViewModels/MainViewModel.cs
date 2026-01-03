using LocalSpaceManager.Core.Interfaces;
using LocalSpaceManager.Core.Models;
using LocalSpaceManager.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace LocalSpaceManager.UI.ViewModels;

/// <summary>
/// Main view model for the application
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly IFileRepository _fileRepository;
    private readonly BackgroundScanService _scanService;
    private bool _isScanning;
    private string _statusMessage = "Ready";
    private string _dbSizeText = "DB Size: 0 B";
    private double _progressValue;
    private string _progressText = string.Empty;
    private string _timeRemainingText = string.Empty;
    private int _currentPage = 0;
    private const int PageSize = 1000;
    private string _sortMode = "Size";
    
    public ObservableCollection<FileInfoModel> Files { get; } = new();
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            _isScanning = value;
            OnPropertyChanged();
        }
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }
    
    public string DbSizeText
    {
        get => _dbSizeText;
        set
        {
            _dbSizeText = value;
            OnPropertyChanged();
        }
    }
    
    public double ProgressValue
    {
        get => _progressValue;
        set
        {
            _progressValue = value;
            OnPropertyChanged();
        }
    }
    
    public string ProgressText
    {
        get => _progressText;
        set
        {
            _progressText = value;
            OnPropertyChanged();
        }
    }
    
    public string TimeRemainingText
    {
        get => _timeRemainingText;
        set
        {
            _timeRemainingText = value;
            OnPropertyChanged();
        }
    }
    
    public string SortMode
    {
        get => _sortMode;
        set
        {
            _sortMode = value;
            OnPropertyChanged();
            _ = LoadFilesAsync();
        }
    }
    
    public ICommand StartScanCommand { get; }
    public ICommand LoadMoreCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SortBySizeCommand { get; }
    public ICommand SortByDateCommand { get; }
    
    public MainViewModel(IFileRepository fileRepository, BackgroundScanService scanService)
    {
        _fileRepository = fileRepository;
        _scanService = scanService;
        
        StartScanCommand = new RelayCommand(async () => await StartScanAsync());
        LoadMoreCommand = new RelayCommand(async () => await LoadMoreFilesAsync());
        RefreshCommand = new RelayCommand(async () => await RefreshFilesAsync());
        SortBySizeCommand = new RelayCommand(() => SortMode = "Size");
        SortByDateCommand = new RelayCommand(() => SortMode = "Date");
        
        _scanService.ScanProgressChanged += OnScanProgressChanged;
        _scanService.ScanCompleted += OnScanCompleted;
        
        _ = LoadFilesAsync();
    }
    
    private async Task StartScanAsync()
    {
        IsScanning = true;
        StatusMessage = "Scanning...";
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
    
    private async Task LoadFilesAsync()
    {
        _currentPage = 0;
        Files.Clear();
        await LoadMoreFilesAsync();
    }
    
    private async Task LoadMoreFilesAsync()
    {
        try
        {
            var skip = _currentPage * PageSize;
            IEnumerable<FileInfoModel> files;
            
            if (SortMode == "Size")
            {
                files = await _fileRepository.GetAllFilesOrderedBySizeAsync(skip, PageSize);
            }
            else
            {
                files = await _fileRepository.GetAllFilesOrderedByDateAsync(skip, PageSize);
            }
            
            foreach (var file in files)
            {
                Files.Add(file);
            }
            
            _currentPage++;
            
            var totalFiles = await _fileRepository.GetTotalFilesCountAsync();
            var totalSize = await _fileRepository.GetTotalSizeAsync();
            UpdateDbSize();
            StatusMessage = $"Showing {Files.Count} of {totalFiles:N0} files | Total: {FormatBytes(totalSize)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading files: {ex.Message}";
        }
    }
    
    private async Task RefreshFilesAsync()
    {
        await LoadFilesAsync();
    }
    
    private void OnScanProgressChanged(object? sender, ScanProgress progress)
    {
        ProgressValue = progress.PercentComplete;
        ProgressText = $"{progress.FilesScanned:N0} files found ({FormatBytes(progress.TotalBytesScanned)})";
        
        if (progress.RemainingTime.HasValue && progress.RemainingTime.Value > TimeSpan.Zero)
        {
            TimeRemainingText = $"Estimated time remaining: {FormatTimeSpan(progress.RemainingTime.Value)}";
        }
        else
        {
            TimeRemainingText = string.Empty;
        }

        StatusMessage = $"Scanning: {progress.CurrentPath}";
    }
    
    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }
    
    private void OnScanCompleted(object? sender, EventArgs e)
    {
        IsScanning = false;
        StatusMessage = "Scan completed";
        UpdateDbSize();
        _ = LoadFilesAsync();
    }
    
    private void UpdateDbSize()
    {
        try
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LocalSpaceManager",
                "localspace.db");
            
            if (File.Exists(dbPath))
            {
                var info = new FileInfo(dbPath);
                DbSizeText = $"DB Size: {FormatBytes(info.Length)}";
            }
        }
        catch { /* Ignore */ }
    }
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Simple relay command implementation
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;
    
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }
    
    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
    
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    
    public void Execute(object? parameter) => _execute();
}
