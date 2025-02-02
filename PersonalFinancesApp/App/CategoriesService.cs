using PersonalFinances.Models;
using PersonalFinances.Repositories;
namespace PersonalFinances.App;

public class CategoriesService : ICategoriesService
{
    Dictionary<string, string> CategoriesMap {get; set;}
    ICategoriesRepository _categoriesRepository;
    ITransactionsUserInteraction _transactionUserInteraction;

    public CategoriesService(
        ICategoriesRepository categoriesRepository,
        ITransactionsUserInteraction transactionUserInteraction)
    {
        _categoriesRepository = categoriesRepository;
        _transactionUserInteraction = transactionUserInteraction;
        CategoriesMap = categoriesRepository.LoadCategoriesMap();
    }

    public string GetCategory(string vendor)
    {
        foreach (var kvp in CategoriesMap)
        {
            if (vendor is not null && vendor.ToLower().Contains(kvp.Key.ToLower()))
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
}

