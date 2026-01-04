# Local Space Manager v2

A high-performance Windows desktop application for monitoring and managing disk storage. The application helps you identify space-consuming directories, assess cleanup risks, and find high-value targets for deletion.

## New in v2: Directory-First Analysis

The core experience has been redesigned to focus on **Directory-based Analysis** and **Risk Assessment**, making it easier to decide what to clean up.

### Key Features

**Directory-First View**: The default view now shows your disk space usage by directory. Quickly identify which folders are consuming the most space without being overwhelmed by individual files.

**Drill-Down Navigation**: Click on any directory to see its subdirectories or switch to a file-level view for that specific folder.

**Risk Assessment Engine**: Every directory and file is assigned a **Risk Level** (Safe, Review, High Risk) with a clear explanation.
- **Safe**: Usually temporary files or common media.
- **Review**: Application data or user settings.
- **High Risk**: System files or critical application components.

**High-Value Cleanup View**: A dedicated view that highlights "Large and Old" files—those that take up significant space and haven't been modified for a long time (e.g., >500MB and >180 days).

**Real-Time Monitoring**: The app continues to monitor your drives in the background, updating the database as files change with minimal resource usage.

## Technical Architecture

### Technology Stack
- **.NET 10.0**: Latest high-performance runtime.
- **WPF**: Native Windows UI with MVVM architecture.
- **Entity Framework Core + SQLite**: Efficient local metadata storage.
- **Risk Engine**: Configurable rule-based assessment for safety.

### Project Structure
- **LocalSpaceManager.Core**: Business logic, Risk Engine, and Interfaces.
- **LocalSpaceManager.Data**: EF Core context and Repository.
- **LocalSpaceManager.UI**: WPF Views, ViewModels, and Converters.

## Getting Started

### Prerequisites
- Windows 10 or 11 (64-bit)
- .NET 10.0 SDK

### Building and Running
```bash
dotnet build --configuration Release
dotnet run --project src/LocalSpaceManager.UI/LocalSpaceManager.UI.csproj
```

## Configuration

You can customize the risk rules and cleanup thresholds in the configuration file:
`%LOCALAPPDATA%\LocalSpaceManager\risk_config.json`

Default thresholds:
- **Large File**: > 500 MB
- **Old File**: > 180 Days

## Usage Guide

1. **Initial Scan**: Click "Scan Drives" to analyze your system. This will populate the directory and file database.
2. **Explore**: Use the directory list to find "space hogs". Click a directory to go deeper.
3. **Assess**: Check the "Risk" column before deleting anything. Hover over the risk badge to see the explanation.
4. **Cleanup**: Use the "Cleanup View" to find the best candidates for deletion (large files you haven't touched in months).

## Database & Logs
- **Database**: `%LOCALAPPDATA%\LocalSpaceManager\localspace.db`
- **Config**: `%LOCALAPPDATA%\LocalSpaceManager\risk_config.json`
- **Logs**: `fatal_error.log` in the application directory.

---
**Version**: 2.0.0  
**Last Updated**: January 2026
