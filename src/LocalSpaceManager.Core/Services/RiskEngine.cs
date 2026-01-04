using LocalSpaceManager.Core.Interfaces;
using LocalSpaceManager.Core.Models;
using System.Text.Json;

namespace LocalSpaceManager.Core.Services;

public class RiskEngine : IRiskEngine
{
    private RiskConfig _config;
    private readonly string _configPath;

    public RiskEngine()
    {
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalSpaceManager",
            "risk_config.json");
        
        _config = LoadConfig();
    }

    public (RiskLevel Level, string Explanation) GetRisk(string path, bool isDirectory)
    {
        // Default to Safe
        var result = (Level: RiskLevel.Safe, Explanation: "User data or common file type.");

        // Check path-based rules first (more specific)
        foreach (var rule in _config.Rules.Where(r => !r.IsExtension))
        {
            if (path.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase))
            {
                return (rule.Level, rule.Explanation);
            }
        }

        // Check extension-based rules
        if (!isDirectory)
        {
            var ext = Path.GetExtension(path).ToLower();
            foreach (var rule in _config.Rules.Where(r => r.IsExtension))
            {
                if (ext == rule.Pattern.ToLower())
                {
                    return (rule.Level, rule.Explanation);
                }
            }
        }

        return result;
    }

    public string GetCategory(string extension)
    {
        extension = extension.ToLower().TrimStart('.');
        return extension switch
        {
            "mp4" or "mkv" or "avi" or "mov" => "Video",
            "mp3" or "wav" or "flac" or "m4a" => "Audio",
            "jpg" or "jpeg" or "png" or "gif" or "bmp" => "Image",
            "zip" or "rar" or "7z" or "tar" or "gz" => "Archive",
            "exe" or "msi" or "bat" or "sh" => "Executable",
            "log" or "txt" or "md" => "Document/Log",
            "pdf" or "doc" or "docx" or "xls" or "xlsx" => "Office",
            "dll" or "sys" or "bin" => "System",
            _ => "Other"
        };
    }

    public RiskConfig GetConfig() => _config;

    public void UpdateConfig(RiskConfig config)
    {
        _config = config;
        SaveConfig();
    }

    private RiskConfig LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<RiskConfig>(json) ?? GetDefaultConfig();
            }
        }
        catch { }

        var defaultConfig = GetDefaultConfig();
        SaveConfig(defaultConfig);
        return defaultConfig;
    }

    private void SaveConfig(RiskConfig? config = null)
    {
        try
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(config ?? _config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
        catch { }
    }

    private RiskConfig GetDefaultConfig()
    {
        return new RiskConfig
        {
            Rules = new List<RiskRule>
            {
                // High Risk
                new() { Pattern = "C:\\Windows", Level = RiskLevel.HighRisk, Explanation = "System directory, deletion will break Windows.", IsExtension = false },
                new() { Pattern = "C:\\Program Files", Level = RiskLevel.HighRisk, Explanation = "Installed applications, should be uninstalled via Settings.", IsExtension = false },
                new() { Pattern = ".sys", Level = RiskLevel.HighRisk, Explanation = "System driver file.", IsExtension = true },
                new() { Pattern = ".dll", Level = RiskLevel.HighRisk, Explanation = "Application library, required for programs to run.", IsExtension = true },
                
                // Review
                new() { Pattern = "AppData", Level = RiskLevel.Review, Explanation = "Application data and settings. Deleting may reset app state.", IsExtension = false },
                new() { Pattern = ".exe", Level = RiskLevel.Review, Explanation = "Executable program. Ensure you don't need this app.", IsExtension = true },
                new() { Pattern = "Temp", Level = RiskLevel.Safe, Explanation = "Temporary files, usually safe to delete.", IsExtension = false },
                new() { Pattern = ".log", Level = RiskLevel.Safe, Explanation = "Log file, typically safe to delete.", IsExtension = true }
            }
        };
    }
}
