using PersonalFinances.Models;

namespace PersonalFinances.App;

public interface IBudgetService
{
    Task<BudgetProfile?> GetProfileAsync(string profileName);
    Task<BudgetProfile?> GetProfileByIdAsync(int id);
    Task StoreProfileAsync(BudgetProfile profile);
    Task<BudgetProfile> CreateNewProfileAsync();
    Task<BudgetProfile?> GetActiveProfileAsync();
    double GetBudgetTotal(BudgetProfile profile);
    Task<List<BudgetProfile>> LoadProfilesAsync();
    Task MigrateProfilesToDatabaseAsync();
}
