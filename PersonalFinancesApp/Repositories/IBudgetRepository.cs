namespace PersonalFinances.Repositories;

public interface IBudgetRepository
{
    void SaveBudgetProfiles(List<BudgetProfile> profiles);
    List<BudgetProfile> LoadBudgetProfiles();
}
