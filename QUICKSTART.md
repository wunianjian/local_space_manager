# Quick Start Guide

Get up and running with Local Space Manager in just a few minutes.

## Installation

### Option 1: Run from Source (Developers)

If you have .NET 10.0 SDK installed:

```bash
git clone https://github.com/yourusername/local_space_manager.git
cd local_space_manager
dotnet run --project src/LocalSpaceManager.UI/LocalSpaceManager.UI.csproj
```

### Option 2: Download Executable (End Users)

Download the latest release from the GitHub releases page and extract the ZIP file to a folder of your choice. Run `LocalSpaceManager.UI.exe` to start the application.

## First-Time Setup

### Step 1: Launch the Application

When you first launch Local Space Manager, you'll see an empty file list. This is normal because the application hasn't scanned your drives yet.

### Step 2: Perform Initial Scan

Click the **🔍 Scan Drives** button in the toolbar. The application will begin scanning all fixed drives on your computer. A progress overlay will appear showing:
- Number of files scanned
- Total bytes scanned
- Current path being processed

**Note**: The initial scan may take 5-30 minutes depending on how many files you have. Be patient and let it complete.

### Step 3: Browse Your Files

Once the scan completes, the file list will populate with all your files. By default, files are sorted by size (largest first).

## Basic Usage

### Viewing Files

The main window displays a table with the following columns:
- **File Name**: The name of the file
- **Directory**: The full path to the file's location
- **Size**: Human-readable file size (B, KB, MB, GB, TB)
- **Modified Date**: When the file was last modified
- **Extension**: File extension (e.g., .txt, .pdf, .jpg)

Scroll through the list to browse all files. The application uses virtualization, so even with millions of files, scrolling remains smooth.

### Sorting Files

Use the toolbar buttons to change how files are sorted:

**Sort by Size**: Click this button to view files ordered by size, with the largest files at the top. This is useful for finding files that consume the most disk space.

**Sort by Modified Date**: Click this button to view files ordered by modification date, with the most recently modified files at the top. This helps you track recent changes to your file system.

### Refreshing the View

Click the **🔄 Refresh** button to reload the file list from the database. This is useful after the background monitor has detected changes.

### Monitoring Status

The status bar at the bottom shows:
- Current view information (e.g., "Showing 1,000 of 250,000 files")
- Total storage size
- Monitoring status (Active/Inactive)

When monitoring is active, the application automatically updates the database when files are created, modified, or deleted.

## Understanding the Background Monitor

### How It Works

After the initial scan completes, Local Space Manager automatically starts monitoring your drives for changes. This happens in the background with minimal resource usage.

When you create, modify, or delete files:
1. The file system monitor detects the change
2. The database is updated automatically
3. Changes appear when you refresh the view

### Resource Usage

The background monitor is designed to be lightweight:
- **Memory**: Typically uses less than 50 MB
- **CPU**: Near zero when idle, brief spikes during file operations
- **Disk**: Only writes to database when changes occur

### What Gets Monitored

The application monitors all fixed local drives (typically C:, D:, etc.). It does not monitor:
- Network drives
- Removable media (USB drives, external hard drives)
- Virtual drives

## Common Tasks

### Finding Large Files

To find files consuming the most space:
1. Click **Sort by Size** in the toolbar
2. The largest files will appear at the top of the list
3. Note the directory path to locate the files

### Finding Recent Changes

To see recently modified files:
1. Click **Sort by Modified Date** in the toolbar
2. The most recently changed files appear at the top
3. Use this to track what's been updated recently

### Rescanning Your Drives

If you want to perform a fresh scan:
1. Click **🔍 Scan Drives** in the toolbar
2. The application will clear the existing database
3. A new scan will begin from scratch

**Warning**: This will delete all existing data and rescan everything, which may take time.

## Tips and Best Practices

### Let the Initial Scan Complete

Don't interrupt the initial scan. Let it run to completion for accurate results. You can minimize the window and continue working while it scans in the background.

### Use Sorting Strategically

Switch between size and date sorting depending on what you're looking for:
- Use **size sorting** when cleaning up disk space
- Use **date sorting** when tracking recent activity

### Check Monitoring Status

Always verify that "Monitoring: Active" appears in the status bar. This ensures the database stays up-to-date with file system changes.

### Refresh Periodically

While the background monitor updates the database automatically, click Refresh to see the latest changes in the UI.

## Troubleshooting

### Scan Is Taking Too Long

The initial scan time depends on the number of files. Systems with millions of files may take 30+ minutes. This is normal. Monitor the progress in the status bar.

### Some Files Are Missing

The application skips files it doesn't have permission to access. This includes some system files and protected directories. This is normal behavior to prevent errors.

### Application Uses Too Much Memory

If memory usage seems high, try closing and reopening the application. The virtualized list should prevent excessive memory use, but a restart can help.

### Database Errors

If you see database errors, try deleting the database file at:
```
%LOCALAPPDATA%\LocalSpaceManager\localspace.db
```
Then restart the application and perform a new scan.

## Next Steps

Once you're comfortable with the basics:
- Explore the full README.md for detailed information
- Check BUILD.md if you want to build from source
- Review ARCHITECTURE.md to understand the technical design

## Getting Help

If you encounter issues:
1. Check the Troubleshooting section above
2. Review the full README.md documentation
3. Open an issue on the GitHub repository with details

---

**Enjoy using Local Space Manager!**
