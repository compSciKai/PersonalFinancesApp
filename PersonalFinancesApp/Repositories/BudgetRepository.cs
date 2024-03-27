using System.Text.Json;
namespace PersonalFinances.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private string _jsonFilePath;

    public BudgetRepository(string jsonFilePath)
    {
        _jsonFilePath = jsonFilePath;
    }

    public List<BudgetProfile> LoadBudgetProfiles()
    {
        if (File.Exists(_jsonFilePath))
        {
            string json = File.ReadAllText(_jsonFilePath);
            if (json != "")
            {
                var profiles = JsonSerializer.Deserialize<List<BudgetProfile>>(json);
                if (profiles is not null)
                {
                    return profiles;
                }
            }
        }

        return new List<BudgetProfile>();
    }

    public void SaveBudgetProfiles(List<BudgetProfile> profiles)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(profiles, options);
        File.WriteAllText(_jsonFilePath, json); 
    }
}
