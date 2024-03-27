using PersonalFinances.Models;
using PersonalFinances.Repositories;

namespace PersonalFinances.App;

public interface IBudgetService
{
    BudgetProfile? GetProfile(string profileName);
    void StoreProfile(BudgetProfile profile);
    BudgetProfile CreateNewProfile();
    BudgetProfile? GetActiveProfile();
}
