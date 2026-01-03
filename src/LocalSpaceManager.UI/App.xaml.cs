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
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Configure dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        
        // Initialize database
        InitializeDatabase();
        
        // Show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        // Start background monitoring
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
        _serviceProvider?.GetRequiredService<BackgroundScanService>().Dispose();
        _serviceProvider?.Dispose();
        base.OnExit(e);
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
        using var scope = _serviceProvider!.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LocalSpaceDbContext>();
        
        // Create database if it doesn't exist
        context.Database.EnsureCreated();
        
        // Apply any pending migrations
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }
}
