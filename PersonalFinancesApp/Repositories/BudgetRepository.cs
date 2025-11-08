using System.Text.Json;
using PersonalFinances.Models;

namespace PersonalFinances.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private string _jsonFilePath;

    public BudgetRepository(string jsonFilePath)
    {
        _jsonFilePath = jsonFilePath;
    }

    public async Task<List<BudgetProfile>> LoadBudgetProfilesAsync()
    {
        if (File.Exists(_jsonFilePath))
        {
            string json = await File.ReadAllTextAsync(_jsonFilePath);
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

    public async Task SaveBudgetProfilesAsync(List<BudgetProfile> profiles)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(profiles, options);
        await File.WriteAllTextAsync(_jsonFilePath, json);
    }

    public async Task<BudgetProfile?> GetProfileByIdAsync(int id)
    {
        var profiles = await LoadBudgetProfilesAsync();
        return profiles.FirstOrDefault(p => p.Id == id);
    }

    public async Task<BudgetProfile?> GetProfileByNameAsync(string name)
    {
        var profiles = await LoadBudgetProfilesAsync();
        return profiles.FirstOrDefault(p => p.Name == name);
    }

    public async Task SaveBudgetProfileAsync(BudgetProfile profile)
    {
        var profiles = await LoadBudgetProfilesAsync();
        var existing = profiles.FirstOrDefault(p => p.Id == profile.Id || p.Name == profile.Name);

        if (existing != null)
        {
            profiles.Remove(existing);
        }

        profiles.Add(profile);
        await SaveBudgetProfilesAsync(profiles);
    }

    public async Task DeleteBudgetProfileAsync(int id)
    {
        var profiles = await LoadBudgetProfilesAsync();
        var profile = profiles.FirstOrDefault(p => p.Id == id);

        if (profile != null)
        {
            profiles.Remove(profile);
            await SaveBudgetProfilesAsync(profiles);
        }
    }
}
