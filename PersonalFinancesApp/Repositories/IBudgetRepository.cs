using PersonalFinances.Models;

namespace PersonalFinances.Repositories;

public interface IBudgetRepository
{
    Task SaveBudgetProfilesAsync(List<BudgetProfile> profiles);
    Task<List<BudgetProfile>> LoadBudgetProfilesAsync();
    Task<BudgetProfile?> GetProfileByIdAsync(int id);
    Task<BudgetProfile?> GetProfileByNameAsync(string name);
    Task SaveBudgetProfileAsync(BudgetProfile profile);
    Task DeleteBudgetProfileAsync(int id);
}
