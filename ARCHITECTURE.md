# Local Space Manager - Architecture Design

## Overview

A Windows desktop application that monitors disk storage, maintains a local database of file metadata, and provides efficient file browsing capabilities with sorting by size and modification date.

## Technology Stack

### Core Technologies
- **Language**: C# with .NET 8.0
- **UI Framework**: WPF (Windows Presentation Foundation) for native Windows experience
- **Database**: SQLite for lightweight, embedded database
- **ORM**: Entity Framework Core for database operations

### Key Libraries
- **FileSystemWatcher**: Built-in .NET class for monitoring file system changes
- **System.IO**: For file system scanning and operations
- **SQLite-net**: Lightweight SQLite wrapper
- **Topshelf**: For creating Windows services (background operation)

## Architecture Components

### 1. File Scanner Module
- **Purpose**: Initial disk scan and indexing
- **Features**:
  - Multi-threaded scanning for performance
  - Progress reporting
  - Configurable scan paths (drives/folders)
  - Error handling for inaccessible files

### 2. Database Layer
- **Schema**:
  - Files table: path, name, size, modified_date, created_date, extension, directory
  - Scan history: scan_date, duration, files_scanned
  - Configuration: monitored_paths, excluded_paths
- **Indexes**: On size, modified_date, path for fast queries

### 3. File System Monitor
- **Purpose**: Real-time file system change detection
- **Implementation**:
  - Multiple FileSystemWatcher instances for each monitored drive
  - Event buffering to handle rapid changes
  - Debouncing to reduce database writes
  - Low CPU usage through event-driven architecture

### 4. Background Service
- **Purpose**: Run continuously in the system tray
- **Features**:
  - Auto-start with Windows
  - Minimal memory footprint (< 50MB)
  - Graceful shutdown handling
  - Periodic database optimization

### 5. User Interface
- **Main Window**:
  - File list view with virtual scrolling
  - Sort by: Size (descending), Modified Date (newest first)
  - Search/filter capabilities
  - Column headers for sorting
  - Status bar showing total files, total size
- **System Tray**:
  - Icon with context menu
  - Quick stats display
  - Settings access

### 6. Configuration Manager
- **Settings**:
  - Monitored drives/folders
  - Excluded paths (e.g., system folders, temp)
  - Auto-start preference
  - Scan schedule

## Performance Optimizations

### Memory Management
- Use pagination for large result sets
- Dispose resources properly
- Limit FileSystemWatcher buffer size

### CPU Efficiency
- Async/await for I/O operations
- Background thread for scanning
- Event batching for database updates
- Incremental indexing

### Database Optimization
- Proper indexing on query columns
- Batch inserts during initial scan
- WAL mode for concurrent access
- Periodic VACUUM operations

## Project Structure

```
local_space_manager/
├── src/
│   ├── LocalSpaceManager.Core/          # Business logic
│   │   ├── Models/                      # Data models
│   │   ├── Services/                    # Core services
│   │   │   ├── FileScanner.cs
│   │   │   ├── FileSystemMonitor.cs
│   │   │   └── DatabaseService.cs
│   │   └── Interfaces/
│   ├── LocalSpaceManager.Data/          # Database layer
│   │   ├── Context/
│   │   ├── Entities/
│   │   └── Repositories/
│   ├── LocalSpaceManager.UI/            # WPF application
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   └── Resources/
│   └── LocalSpaceManager.Service/       # Windows service
├── tests/
├── docs/
└── README.md
```

## Development Phases

1. **Phase 1**: Core scanning and database setup
2. **Phase 2**: File system monitoring implementation
3. **Phase 3**: UI development
4. **Phase 4**: Background service integration
5. **Phase 5**: Testing and optimization

## Security Considerations

- Run with user privileges (not admin)
- Handle access denied errors gracefully
- Sanitize file paths to prevent injection
- Secure database file location

## Future Enhancements

- Duplicate file detection
- Storage usage visualization (charts)
- File type analysis
- Cleanup suggestions
- Cloud storage integration
