using LocalSpaceManager.Core.Models;

namespace LocalSpaceManager.Core.Interfaces;

public interface IRiskEngine
{
    (RiskLevel Level, string Explanation) GetRisk(string path, bool isDirectory);
    string GetCategory(string extension);
    RiskConfig GetConfig();
    void UpdateConfig(RiskConfig config);
}
