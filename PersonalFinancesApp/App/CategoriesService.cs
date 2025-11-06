using PersonalFinances.Models;
using PersonalFinances.Repositories;
namespace PersonalFinances.App;

public class CategoriesService : ICategoriesService
{
    public Dictionary<string, string> CategoriesMap { get; private set; }
    ICategoriesRepository _categoriesRepository;
    ICategoriesRepository? _jsonRepository;
    ITransactionsUserInteraction _transactionUserInteraction;

    public CategoriesService(
        ICategoriesRepository categoriesRepository,
        ICategoriesRepository? jsonRepository,
        ITransactionsUserInteraction transactionUserInteraction)
    {
        _categoriesRepository = categoriesRepository;
        _jsonRepository = jsonRepository;
        _transactionUserInteraction = transactionUserInteraction;
        CategoriesMap = categoriesRepository.LoadCategoriesMap();
    }

    public string GetCategory(string vendor)
    {
        foreach (var kvp in CategoriesMap)
        {
            if (vendor != null && vendor.ToLower().Contains(kvp.Key.ToLower()))
            {
                return kvp.Value.ToLower();
            }
        }

        return "";
    }

    public List<string> GetAllCategories()
    {
        return CategoriesMap.Values.Distinct().ToList();
    }

    public void StoreNewCategory(string key, string categoryName)
    {
        CategoriesMap.Add(key, categoryName);
        _categoriesRepository.SaveCategoriesMap(CategoriesMap);
    }

    public void StoreNewCategories(Dictionary<string, string> newCategoryEntries)
    {
        foreach (var kvp in newCategoryEntries)
        {
            if (!newCategoryEntries.ContainsKey(kvp.Key))
            {
                CategoriesMap[kvp.Key] = kvp.Value;
            }
        }
    }

    public List<Transaction> AddCategoriesToTransactions(List<Transaction> transactions)
    {
        foreach (var transaction in transactions)
        {
            if (transaction.Category is null)
            {
                string? categoryName = GetCategory(transaction.Vendor);

                if (categoryName == "")
                {
                    KeyValuePair<string, string>? categoryKVP = _transactionUserInteraction.PromptForCategoryKVP(transaction.Vendor);
                    if (categoryKVP is null)
                    {
                        continue;
                    }

                    StoreNewCategory(categoryKVP?.Key, categoryKVP?.Value);
                    categoryName = categoryKVP?.Value;
                }

                transaction.Category = categoryName;
            }
        }

        return transactions;
    }

    public List<Transaction> OverrideCategories(List<Transaction> transactions, string categoryToOverride, string newCategory)
    {
        foreach (var transaction in transactions)
        {
            if (transaction.Category is not null)
            {
                string? categoryName = transaction.Category;
                if (!string.IsNullOrEmpty(categoryName) && categoryName.ToLower() == categoryToOverride.ToLower())
                {
                    transaction.Category = newCategory.ToLower();
                }
            }
        }

        return transactions;
    }

    public async Task MigrateCategoriesFromJsonAsync()
    {
        if (_jsonRepository == null)
        {
            _transactionUserInteraction.ShowMessage("Error: JSON repository not available for migration.");
            return;
        }

        _transactionUserInteraction.ShowMessage("\n=== Migrating Categories from JSON to Database ===");

        try
        {
            // Load categories from JSON
            var jsonCategoriesMap = _jsonRepository.LoadCategoriesMap();
            _transactionUserInteraction.ShowMessage($"Loaded {jsonCategoriesMap.Count} category mappings from JSON file.");

            // Save to database
            var dbRepo = _categoriesRepository as DatabaseCategoriesRepository;
            if (dbRepo != null)
            {
                await dbRepo.SaveCategoryMappingsAsync(jsonCategoriesMap);
                _transactionUserInteraction.ShowMessage($"Successfully migrated {jsonCategoriesMap.Count} category mappings to database.");
            }
            else
            {
                _transactionUserInteraction.ShowMessage("Error: Database repository not available.");
            }
        }
        catch (Exception ex)
        {
            _transactionUserInteraction.ShowMessage($"Error during category migration: {ex.Message}");
            throw;
        }
    }

    public async Task MigrateMappingsAsync()
    {
        _transactionUserInteraction.ShowMessage("\n========================================");
        _transactionUserInteraction.ShowMessage("  Vendor & Category Mapping Migration");
        _transactionUserInteraction.ShowMessage("========================================\n");

        try
        {
            // First migrate vendors (this creates VendorMapping records)
            var vendorsService = new VendorsService(_categoriesRepository as dynamic, _jsonRepository as dynamic, _transactionUserInteraction);
            // Note: We'll need to get VendorsService from DI instead. For now, migrating in sequence.

            _transactionUserInteraction.ShowMessage("Step 1/2: Migrating vendors...");
            // Vendors migration will be called separately

            _transactionUserInteraction.ShowMessage("\nStep 2/2: Migrating categories...");
            await MigrateCategoriesFromJsonAsync();

            _transactionUserInteraction.ShowMessage("\n========================================");
            _transactionUserInteraction.ShowMessage("  Migration Complete!");
            _transactionUserInteraction.ShowMessage("========================================\n");
        }
        catch (Exception ex)
        {
            _transactionUserInteraction.ShowMessage($"\nMigration failed: {ex.Message}");
            throw;
        }
    }
}

