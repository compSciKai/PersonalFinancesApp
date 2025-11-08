using PersonalFinances.Models;

namespace PersonalFinances.App;

public interface ICategoriesService
{
    string GetCategory(string vendor);
    List<string> GetAllCategories();
    Task StoreNewCategoryAsync(string key, string categoryName);
    void StoreNewCategories(Dictionary<string, string> categoryDictionary);
    Task<List<Transaction>> AddCategoriesToTransactionsAsync(List<Transaction> transactions, BudgetProfile? profile, IBudgetService? budgetService);
    List<Transaction> OverrideCategories(List<Transaction> transactions, string categoryToOverride, string newCategory);
    Task MigrateCategoriesFromJsonAsync();
    Task MigrateMappingsAsync();
    Task RunCategoryCleanupAsync(BudgetProfile profile);
}
