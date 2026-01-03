# Local Space Manager

A high-performance Windows desktop application for monitoring and managing disk storage. The application scans all your disk drives, maintains a local database of file metadata, and provides real-time monitoring of file system changes with minimal resource usage.

## Features

### Core Functionality

**Initial Disk Scanning**: The application performs a comprehensive scan of all fixed drives on your system, collecting metadata for every file including name, path, size, creation date, and modification date. The scanning process is multi-threaded and optimized for performance, capable of handling millions of files efficiently.

**Local Database Storage**: All file metadata is stored in a lightweight SQLite database located in your local application data folder. The database uses optimized indexes on size and modification date columns to ensure fast query performance even with large datasets.

**File Ranking and Sorting**: View your files sorted by two primary criteria. The size ranking shows the largest files first, making it easy to identify space-consuming files. The modification date ranking displays the most recently modified files, helping you track recent changes to your file system.

**Real-Time File System Monitoring**: Once the initial scan is complete, the application continuously monitors all scanned drives for changes. When files are created, modified, or deleted, the database is automatically updated in real-time. The monitoring system uses event debouncing and batching to minimize CPU usage and database writes.

**Low Resource Usage**: The background monitoring service is designed to consume minimal system resources. It typically uses less than 50MB of memory and negligible CPU when idle. The event-driven architecture ensures the application only processes changes when they occur.

### User Interface

The main window provides a clean, responsive interface with the following components:

**Toolbar**: Quick access buttons for scanning drives, refreshing the view, and switching between sort modes (size vs. date).

**File List**: A virtualized data grid displaying file information with columns for file name, directory path, size, modification date, and extension. The virtualization ensures smooth scrolling even with hundreds of thousands of files loaded.

**Status Bar**: Real-time status information showing the number of files displayed, total storage size, and monitoring status. During scanning, it displays progress information including files scanned and current path.

**Progress Indicator**: A modal overlay appears during the initial scan operation, showing scanning progress with an animated progress bar.

## Technical Architecture

### Technology Stack

The application is built using modern .NET technologies:

- **.NET 10.0**: The latest long-term support version of .NET for Windows
- **WPF (Windows Presentation Foundation)**: Native Windows UI framework for rich desktop applications
- **Entity Framework Core**: Object-relational mapping for database operations
- **SQLite**: Embedded database engine for local data storage
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection for loose coupling

### Project Structure

The solution is organized into three main projects:

**LocalSpaceManager.Core**: Contains business logic, interfaces, and domain models. This project is framework-agnostic and includes the file scanner, file system monitor, and background scan service.

**LocalSpaceManager.Data**: Handles all database operations using Entity Framework Core. Includes the database context, entity definitions, and repository implementations.

**LocalSpaceManager.UI**: The WPF application project containing views, view models, and application startup logic. Implements the MVVM (Model-View-ViewModel) pattern for clean separation of concerns.

### Key Components

**FileScanner**: Recursively scans directories and collects file metadata. Handles access denied errors gracefully and reports progress during scanning operations.

**FileSystemMonitor**: Wraps the .NET FileSystemWatcher class to monitor multiple drives simultaneously. Implements debouncing to prevent duplicate event processing and manages event buffering for optimal performance.

**BackgroundScanService**: Coordinates between the scanner and monitor. Performs the initial scan, saves results to the database, and starts monitoring. Handles file system events and updates the database accordingly.

**FileRepository**: Provides data access methods for querying and updating file metadata. Implements efficient pagination and sorting using database indexes.

**MainViewModel**: The primary view model that connects the UI to the business logic. Manages the observable collection of files, handles user commands, and updates the UI based on scan progress.

## Getting Started

### Prerequisites

To build and run this application, you need:

- Windows 10 or Windows 11 (64-bit)
- .NET 10.0 SDK or later
- Visual Studio 2022 (recommended) or Visual Studio Code with C# extension

### Building the Application

Open a command prompt or PowerShell window in the project root directory and run:

```bash
dotnet restore
dotnet build --configuration Release
```

The compiled application will be located in:
```
src/LocalSpaceManager.UI/bin/Release/net8.0-windows/
```

### Running the Application

You can run the application directly from Visual Studio by setting `LocalSpaceManager.UI` as the startup project and pressing F5, or from the command line:

```bash
dotnet run --project src/LocalSpaceManager.UI/LocalSpaceManager.UI.csproj
```

For the first run, you'll need to perform an initial scan by clicking the "Scan Drives" button in the toolbar.

## Usage Guide

### Initial Setup

When you first launch the application, the file list will be empty. Click the **Scan Drives** button in the toolbar to begin the initial scan. The application will scan all fixed drives on your system. Depending on the number of files, this process may take several minutes to complete.

During the scan, a progress overlay will display the current status, including the number of files scanned and the current path being processed. You can monitor the progress in the status bar as well.

### Viewing Files

Once the scan is complete, the file list will populate with your files. By default, files are sorted by size in descending order, showing the largest files first. You can scroll through the list to browse all files.

The data grid supports:
- **Column sorting**: Click on column headers to sort by that column
- **Column resizing**: Drag column borders to adjust width
- **Scrolling**: Use mouse wheel or scrollbar to navigate through files

### Sorting Options

Use the toolbar buttons to switch between sort modes:

- **Sort by Size**: Displays files ordered by size, largest first
- **Sort by Modified Date**: Displays files ordered by modification date, most recent first

The view will automatically refresh when you change the sort mode.

### Real-Time Monitoring

After the initial scan, the application automatically starts monitoring your drives for changes. The status bar will show "Monitoring: Active" to indicate that monitoring is running.

When you create, modify, or delete files, the database will be updated automatically within a few seconds. Click the **Refresh** button to reload the file list and see the latest changes.

## Database Location

The SQLite database file is stored at:
```
%LOCALAPPDATA%\LocalSpaceManager\localspace.db
```

On a typical Windows installation, this resolves to:
```
C:\Users\[YourUsername]\AppData\Local\LocalSpaceManager\localspace.db
```

The database file can grow to several hundred megabytes depending on the number of files on your system. You can safely delete this file to reset the application, but you'll need to perform a new initial scan.

## Performance Considerations

### Memory Usage

The application uses virtualization in the UI to minimize memory consumption. Only visible rows in the data grid are kept in memory, allowing you to browse millions of files without excessive memory usage.

The background monitoring service maintains a small in-memory cache for debouncing file system events, typically using less than 10MB of additional memory.

### CPU Usage

During the initial scan, CPU usage will be moderate as the application reads file metadata from disk. Once monitoring begins, CPU usage drops to near zero when idle and spikes briefly only when file system changes occur.

The debouncing mechanism prevents excessive processing when many files change rapidly, such as during a large file copy operation.

### Disk I/O

The application performs read-only operations during scanning and monitoring. Database writes are batched during the initial scan for optimal performance. During monitoring, database updates occur asynchronously to avoid blocking the UI thread.

## Limitations and Known Issues

### Excluded Locations

The current version scans all files on fixed drives. Future versions may add configuration options to exclude specific folders such as:
- System directories (Windows, Program Files)
- Temporary folders
- Recycle Bin
- Virtual machine disk images

### Access Denied

Some system files and folders require administrator privileges to access. The application handles these gracefully by logging the error and continuing with the scan. These files will not appear in the database.

### Network Drives

The current version only monitors fixed local drives. Network drives and removable media are not included in the scan or monitoring.

### Large File Operations

When moving or copying very large files, the file system may generate multiple change events. The debouncing mechanism helps reduce duplicate processing, but you may see temporary inconsistencies in the database during these operations.

## Future Enhancements

Planned features for future versions include:

- **Duplicate File Detection**: Identify files with identical content using hash comparison
- **Storage Visualization**: Charts and graphs showing storage usage by file type, directory, and time
- **File Type Analysis**: Statistics on file types and their space consumption
- **Cleanup Suggestions**: Automated recommendations for files that can be safely deleted
- **Custom Filters**: User-defined filters for viewing specific file types or locations
- **Export Functionality**: Export file lists to CSV or Excel format
- **Search Capabilities**: Full-text search across file names and paths
- **Scheduled Scans**: Automatic periodic rescans to ensure database accuracy

## Troubleshooting

### Application Won't Start

Ensure you have .NET 10.0 Runtime installed. You can download it from the official Microsoft website.

### Scan Takes Too Long

The initial scan time depends on the number of files and disk speed. For systems with millions of files, the scan may take 10-30 minutes. You can monitor progress in the status bar.

### Database Errors

If you encounter database errors, try deleting the database file and performing a fresh scan. The application will automatically recreate the database on next startup.

### High Memory Usage

If memory usage seems excessive, try closing and reopening the application. The virtualized list should prevent memory issues, but in rare cases, a restart may help.

## Contributing

This project is open for contributions. If you'd like to add features or fix bugs, please follow these guidelines:

1. Fork the repository
2. Create a feature branch
3. Make your changes with appropriate tests
4. Submit a pull request with a clear description

## License

This project is provided as-is for personal and educational use. Please refer to the LICENSE file for detailed terms.

## Support

For bug reports and feature requests, please open an issue on the GitHub repository. Include detailed information about your system configuration and steps to reproduce any issues.

---

**Version**: 1.0.0  
**Last Updated**: January 2026  
**Author**: Local Space Manager Development Team
