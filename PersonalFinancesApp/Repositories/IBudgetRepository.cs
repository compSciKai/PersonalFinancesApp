namespace PersonalFinances.Repositories;

public interface IBudgetRepository
{
    void SaveBudgetProfiles(Dictionary<string, BudgetProfile> profiles);
    Dictionary<string, BudgetProfile> LoadBudgetProfiles();
}
