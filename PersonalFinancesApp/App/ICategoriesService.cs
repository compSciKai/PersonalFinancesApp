﻿using PersonalFinances.Models;

namespace PersonalFinances.App;

public interface ICategoriesService
{
    string GetCategory(string vendor);
    List<string> GetAllCategories();
    void StoreNewCategory(string key, string categoryName);
    void StoreNewCategories(Dictionary<string, string> categoryDictionary);
    List<Transaction> AddCategoriesToTransactions(List<Transaction> transactions);
}
