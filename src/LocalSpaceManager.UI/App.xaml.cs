using LocalSpaceManager.Core.Interfaces;
using LocalSpaceManager.Core.Services;
using LocalSpaceManager.Data.Context;
using LocalSpaceManager.Data.Repositories;
using LocalSpaceManager.UI.ViewModels;
using LocalSpaceManager.UI.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;

namespace LocalSpaceManager.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;
    
    private System.Windows.Forms.NotifyIcon? _notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Add global exception handling
        AppDomain.CurrentDomain.UnhandledException += (s, ex) => 
            LogFatalError(ex.ExceptionObject as Exception, "AppDomain.UnhandledException");
        
        DispatcherUnhandledException += (s, ex) => 
        {
            LogFatalError(ex.Exception, "DispatcherUnhandledException");
            ex.Handled = true;
        };

        try 
        {
            // Configure dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            
            // Initialize database
            InitializeDatabase();
            
            // Show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            
            // Initialize System Tray
            InitializeNotifyIcon();
        }
        catch (Exception ex)
        {
            LogFatalError(ex, "Startup Error");
            Shutdown();
        }
        
        // Start background monitoring
        if (_serviceProvider == null) return;
        var scanService = _serviceProvider.GetRequiredService<BackgroundScanService>();
        
        // Monitor all fixed drives by default
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
            .Select(d => d.RootDirectory.FullName);
        
        // Only start monitoring if database already has data
        Task.Run(async () =>
        {
            var repository = _serviceProvider.GetRequiredService<IFileRepository>();
            var fileCount = await repository.GetTotalFilesCountAsync();
            
            if (fileCount > 0)
            {
                scanService.StartMonitoring(drives);
            }
        });
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        // Clean up
        _notifyIcon?.Dispose();
        _serviceProvider?.GetRequiredService<BackgroundScanService>().Dispose();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private void InitializeNotifyIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon();
        _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "Local Space Manager";
        
        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Open", null, (s, e) => ShowMainWindow());
        contextMenu.Items.Add("Exit", null, (s, e) => Shutdown());
        
        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (MainWindow == null)
        {
            MainWindow = _serviceProvider?.GetRequiredService<MainWindow>();
        }
        
        MainWindow?.Show();
        if (MainWindow?.WindowState == System.Windows.WindowState.Minimized)
        {
            MainWindow.WindowState = System.Windows.WindowState.Normal;
        }
        MainWindow?.Activate();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // Database
        services.AddDbContext<LocalSpaceDbContext>(options =>
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LocalSpaceManager",
                "localspace.db");
            
            options.UseSqlite($"Data Source={dbPath}");
        });
        
        // Repositories
        services.AddScoped<IFileRepository, FileRepository>();
        
        // Services
        services.AddSingleton<IRiskEngine, RiskEngine>();
        services.AddSingleton<IFileScanner, FileScanner>();
        services.AddSingleton<IFileSystemMonitor, FileSystemMonitor>();
        services.AddSingleton<BackgroundScanService>();
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        // ViewModels
        services.AddTransient<MainViewModel>();
        
        // Views
        services.AddTransient<MainWindow>();
    }
    
    private void InitializeDatabase()
    {
        try 
        {
            if (_serviceProvider == null) return;

            // Ensure directory exists before initializing database
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LocalSpaceManager",
                "localspace.db");
            
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LocalSpaceDbContext>();
            
            // Create database if it doesn't exist
            context.Database.EnsureCreated();
            
            // Apply any pending migrations
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to initialize database. Check if SQLite is accessible.", ex);
        }
    }

    private void LogFatalError(Exception? ex, string source)
    {
        string message = $"A fatal error occurred in {source}:\n{ex?.Message}\n\n{ex?.StackTrace}";
        System.Windows.MessageBox.Show(message, "Local Space Manager - Fatal Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        
        try 
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fatal_error.log");
            File.AppendAllText(logPath, $"{DateTime.Now}: [{source}] {message}\n\n");
        }
        catch { /* Ignore logging failures */ }
    }
}
