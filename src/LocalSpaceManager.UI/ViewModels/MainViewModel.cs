using LocalSpaceManager.Core.Interfaces;
using LocalSpaceManager.Core.Models;
using LocalSpaceManager.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace LocalSpaceManager.UI.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly BackgroundScanService _scanService;
    private readonly IFileRepository _repository;
    private string _currentView = "Directories";
    private string _currentPath = "All Drives";
    private bool _isScanning;
    private string _statusMessage = "Ready";
    private double _progressValue;
    private string _progressText = string.Empty;
    private string _timeRemainingText = string.Empty;
    private string _dbSizeText = "DB Size: 0 MB";
    private string _selectedDrive = "All";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DirectoryInfoModel> Directories { get; } = new();
    public ObservableCollection<FileInfoModel> Files { get; } = new();
    public ObservableCollection<string> Drives { get; } = new();

    public string CurrentView
    {
        get => _currentView;
        set { _currentView = value; OnPropertyChanged(nameof(CurrentView)); }
    }

    public string CurrentPath
    {
        get => _currentPath;
        set { _currentPath = value; OnPropertyChanged(nameof(CurrentPath)); }
    }

    public bool IsScanning
    {
        get => _isScanning;
        set { _isScanning = value; OnPropertyChanged(nameof(IsScanning)); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
    }

    public double ProgressValue
    {
        get => _progressValue;
        set { _progressValue = value; OnPropertyChanged(nameof(ProgressValue)); }
    }

    public string ProgressText
    {
        get => _progressText;
        set { _progressText = value; OnPropertyChanged(nameof(ProgressText)); }
    }

    public string TimeRemainingText
    {
        get => _timeRemainingText;
        set { _timeRemainingText = value; OnPropertyChanged(nameof(TimeRemainingText)); }
    }

    public string DbSizeText
    {
        get => _dbSizeText;
        set { _dbSizeText = value; OnPropertyChanged(nameof(DbSizeText)); }
    }

    public string SelectedDrive
    {
        get => _selectedDrive;
        set 
        { 
            if (_selectedDrive != value)
            {
                _selectedDrive = value; 
                OnPropertyChanged(nameof(SelectedDrive));
                _ = ShowTopDirectoriesAsync();
            }
        }
    }

    public ICommand StartScanCommand { get; }
    public ICommand NavigateToDirectoryCommand { get; }
    public ICommand NavigateBackCommand { get; }
    public ICommand ViewFilesCommand { get; }
    public ICommand ViewCleanupCommand { get; }
    public ICommand ViewTopDirectoriesCommand { get; }

    public MainViewModel(BackgroundScanService scanService, IFileRepository repository)
    {
        _scanService = scanService;
        _repository = repository;

        StartScanCommand = new RelayCommand(async () => await StartScanAsync());
        NavigateToDirectoryCommand = new RelayCommand<DirectoryInfoModel>(async (dir) => await NavigateToDirectoryAsync(dir));
        NavigateBackCommand = new RelayCommand(async () => await NavigateBackAsync());
        ViewFilesCommand = new RelayCommand(async () => await ShowFilesAsync());
        ViewCleanupCommand = new RelayCommand(async () => await ShowCleanupAsync());
        ViewTopDirectoriesCommand = new RelayCommand(async () => await ShowTopDirectoriesAsync());

        _scanService.ScanProgressChanged += OnScanProgressChanged;
        _scanService.ScanCompleted += OnScanCompleted;

        // Load initial data after a short delay to ensure DB is ready
        Task.Run(async () => 
        {
            await Task.Delay(500);
            await LoadDrivesAsync();
            await ShowTopDirectoriesAsync();
        });
    }

    private async Task LoadDrivesAsync()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed)
            .Select(d => d.Name)
            .ToList();

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Drives.Clear();
            Drives.Add("All");
            foreach (var drive in drives) Drives.Add(drive);
        });
    }

    private async Task StartScanAsync()
    {
        IsScanning = true;
        StatusMessage = "Initializing scan...";
        
        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed)
            .Select(d => d.Name)
            .ToList();

        try
        {
            await _scanService.InitialScanAsync(drives);
            await LoadDrivesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Scan failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsScanning = false;
            await UpdateDbSizeAsync();
        }
    }

    public async Task ShowTopDirectoriesAsync()
    {
        CurrentView = "Directories";
        CurrentPath = SelectedDrive == "All" ? "All Drives" : SelectedDrive;
        
        var dirs = await _repository.GetTopDirectoriesAsync(100, SelectedDrive == "All" ? null : SelectedDrive);
        
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Directories.Clear();
            foreach (var dir in dirs) Directories.Add(dir);
        });
        
        await UpdateDbSizeAsync();
    }

    private async Task NavigateToDirectoryAsync(DirectoryInfoModel? directory)
    {
        if (directory == null) return;

        CurrentPath = directory.FullPath;
        var subDirs = await _repository.GetSubDirectoriesAsync(directory.FullPath);
        
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Directories.Clear();
            foreach (var dir in subDirs) Directories.Add(dir);
        });
    }

    private async Task NavigateBackAsync()
    {
        if (CurrentPath == "All Drives" || (SelectedDrive != "All" && CurrentPath == SelectedDrive)) return;

        var parentPath = Path.GetDirectoryName(CurrentPath);
        if (string.IsNullOrEmpty(parentPath))
        {
            await ShowTopDirectoriesAsync();
        }
        else
        {
            var parentDir = new DirectoryInfoModel { FullPath = parentPath };
            await NavigateToDirectoryAsync(parentDir);
        }
    }

    private async Task ShowFilesAsync()
    {
        CurrentView = "Files";
        var files = await _repository.GetFilesByDirectoryAsync(CurrentPath);
        
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Files.Clear();
            foreach (var file in files) Files.Add(file);
        });
    }

    private async Task ShowCleanupAsync()
    {
        CurrentView = "Cleanup";
        CurrentPath = "High-Value Cleanup Targets";
        
        // Default thresholds: > 500MB and > 180 days
        var files = await _repository.GetLargeOldFilesAsync(500 * 1024 * 1024, 180);
        
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Files.Clear();
            foreach (var file in files) Files.Add(file);
        });
    }

    private async Task UpdateDbSizeAsync()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalSpaceManager", "localspace.db");
        if (File.Exists(dbPath))
        {
            var size = new FileInfo(dbPath).Length / (1024.0 * 1024.0);
            DbSizeText = $"DB Size: {size:F1} MB";
        }
    }

    private void OnScanProgressChanged(object? sender, ScanProgress e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ProgressValue = e.PercentComplete;
            ProgressText = $"{e.FilesScanned:N0} files found...";
            StatusMessage = e.CurrentPath;
            TimeRemainingText = e.RemainingTime.HasValue ? $"Est. time remaining: {e.RemainingTime.Value:mm\\:ss}" : "Calculating time...";
        });
    }

    private void OnScanCompleted(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsScanning = false;
            StatusMessage = "Scan complete";
            _ = ShowTopDirectoriesAsync();
        });
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    public RelayCommand(Action execute) => _execute = execute;
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    public RelayCommand(Action<T?> execute) => _execute = execute;
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute((T?)parameter);
}
