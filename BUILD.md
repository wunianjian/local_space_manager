# Build Instructions

This document provides detailed instructions for building and running the Local Space Manager application.

## Prerequisites

Before building the application, ensure you have the following installed on your Windows machine:

### Required Software

**Microsoft .NET 10.0 SDK** or later is required to build the application. You can download it from the official Microsoft website at https://dotnet.microsoft.com/download/dotnet/10.0. Choose the SDK installer for Windows x64.

**Visual Studio 2022** (recommended) provides the best development experience for this WPF application. The Community Edition is free and includes all necessary components. During installation, ensure you select the ".NET desktop development" workload.

Alternatively, you can use **Visual Studio Code** with the C# extension, though Visual Studio 2022 is recommended for WPF development.

## Building from Command Line

### Step 1: Clone the Repository

If you haven't already cloned the repository, use Git to clone it:

```bash
git clone https://github.com/yourusername/local_space_manager.git
cd local_space_manager
```

### Step 2: Restore Dependencies

Restore all NuGet packages required by the solution:

```bash
dotnet restore
```

This command will download all necessary packages including Entity Framework Core, SQLite, and WPF dependencies.

### Step 3: Build the Solution

Build the entire solution in Release configuration:

```bash
dotnet build --configuration Release
```

For a Debug build, simply omit the configuration parameter:

```bash
dotnet build
```

### Step 4: Run the Application

Run the application directly using the dotnet CLI:

```bash
dotnet run --project src/LocalSpaceManager.UI/LocalSpaceManager.UI.csproj
```

## Building from Visual Studio

### Step 1: Open the Solution

Launch Visual Studio 2022 and open the solution file:
```
LocalSpaceManager.sln
```

### Step 2: Restore NuGet Packages

Visual Studio should automatically restore NuGet packages when you open the solution. If not, right-click on the solution in Solution Explorer and select "Restore NuGet Packages".

### Step 3: Set Startup Project

In Solution Explorer, right-click on the `LocalSpaceManager.UI` project and select "Set as Startup Project". This ensures the WPF application launches when you press F5.

### Step 4: Build the Solution

From the menu bar, select:
- **Build → Build Solution** (or press Ctrl+Shift+B)

This will compile all three projects in the solution.

### Step 5: Run the Application

Press **F5** to run the application in debug mode, or **Ctrl+F5** to run without debugging.

## Creating a Standalone Executable

To create a self-contained executable that can run on any Windows machine without requiring .NET to be installed:

### Single-File Executable

```bash
dotnet publish src/LocalSpaceManager.UI/LocalSpaceManager.UI.csproj ^
  --configuration Release ^
  --runtime win-x64 ^
  --self-contained true ^
  --output ./publish ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true
```

This creates a single executable file in the `./publish` directory that includes the .NET runtime and all dependencies.

### Framework-Dependent Executable

If you prefer a smaller executable that requires .NET 10.0 to be installed on the target machine:

```bash
dotnet publish src/LocalSpaceManager.UI/LocalSpaceManager.UI.csproj ^
  --configuration Release ^
  --runtime win-x64 ^
  --self-contained false ^
  --output ./publish
```

## Troubleshooting Build Issues

### Missing SDK

If you see an error about missing SDK, ensure you have .NET 8.0 SDK installed. Check your installed SDKs with:

```bash
dotnet --list-sdks
```

You should see version 10.0.x in the list.

### NuGet Package Restore Failures

If package restoration fails, try clearing the NuGet cache:

```bash
dotnet nuget locals all --clear
dotnet restore
```

### Build Errors in Entity Framework

If you encounter errors related to Entity Framework or database context, ensure all three projects are building successfully. The UI project depends on both Core and Data projects.

### WPF Designer Issues

If the WPF designer in Visual Studio shows errors, try:
1. Clean the solution (Build → Clean Solution)
2. Rebuild the solution (Build → Rebuild Solution)
3. Restart Visual Studio

## Running Tests

Currently, the solution does not include automated tests. Future versions will include unit tests for core functionality. To prepare for testing:

```bash
dotnet test
```

## Development Workflow

### Recommended Development Process

When making changes to the application, follow this workflow:

1. Make code changes in the appropriate project (Core, Data, or UI)
2. Build the solution to check for compilation errors
3. Run the application to test your changes
4. Use the debugger to step through code if needed
5. Commit your changes with descriptive commit messages

### Database Schema Changes

If you modify the database entities in the Data project, you'll need to create a migration:

```bash
dotnet ef migrations add MigrationName --project src/LocalSpaceManager.Data --startup-project src/LocalSpaceManager.UI
```

Apply the migration:

```bash
dotnet ef database update --project src/LocalSpaceManager.Data --startup-project src/LocalSpaceManager.UI
```

Note: The application uses `EnsureCreated()` for simplicity, which doesn't use migrations. For production use, consider switching to proper migrations.

## Performance Optimization

### Release Build Optimizations

The Release configuration includes several optimizations:
- Code optimization enabled
- Debug symbols removed
- Smaller output size
- Better runtime performance

Always test with Release builds before distributing the application.

### AOT Compilation

For even better startup performance, you can enable ReadyToRun compilation:

```bash
dotnet publish src/LocalSpaceManager.UI/LocalSpaceManager.UI.csproj ^
  --configuration Release ^
  --runtime win-x64 ^
  /p:PublishReadyToRun=true
```

This increases the output size but improves startup time.

## Deployment

### Installer Creation

For professional deployment, consider creating an installer using:
- **WiX Toolset**: For MSI installers
- **Inno Setup**: For simple setup executables
- **ClickOnce**: For web-based deployment

### System Requirements for End Users

Inform users of the following requirements:
- Windows 10 version 1809 or later, or Windows 11
- 100 MB free disk space for application
- Additional space for database (varies by number of files)
- Administrator privileges may be required for installation

## Additional Resources

- [.NET 10.0 Documentation](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10)
- [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)

---

For questions or issues with building, please open an issue on the GitHub repository.
