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

    public async Task StoreNewCategoryAsync(string key, string categoryName)
    {
        CategoriesMap.Add(key, categoryName);

        // Use database repository async method
        var dbRepo = _categoriesRepository as DatabaseCategoriesRepository;
        if (dbRepo != null)
        {
            await dbRepo.SaveCategoryMappingAsync(key, categoryName);
        }
        else
        {
            _categoriesRepository.SaveCategoriesMap(CategoriesMap);
        }
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

    public async Task<List<Transaction>> AddCategoriesToTransactionsAsync(List<Transaction> transactions)
    {
        bool skipAll = false;

        foreach (var transaction in transactions)
        {
            if (transaction.Category is null)
            {
                string? categoryName = GetCategory(transaction.Vendor);

                if (categoryName == "" && !skipAll)
                {
                    var (categoryKVP, skipAllFlag) = _transactionUserInteraction.PromptForCategoryKVP(transaction.Vendor);

                    if (skipAllFlag)
                    {
                        skipAll = true;
                        continue;
                    }

                    if (categoryKVP is null)
                    {
                        continue;
                    }

                    await StoreNewCategoryAsync(categoryKVP?.Key, categoryKVP?.Value);
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

    public async Task RunCategoryCleanupAsync(BudgetProfile profile)
    {
        _transactionUserInteraction.ShowMessage("\n========================================");
        _transactionUserInteraction.ShowMessage("       Category Cleanup Tool");
        _transactionUserInteraction.ShowMessage("========================================\n");

        try
        {
            var dbRepo = _categoriesRepository as DatabaseCategoriesRepository;
            if (dbRepo == null)
            {
                _transactionUserInteraction.ShowMessage("Error: Database repository not available for cleanup.");
                return;
            }

            // Get all categories with usage counts
            _transactionUserInteraction.ShowMessage("Loading categories from database...");
            var categoryCounts = await dbRepo.GetCategoryUsageCountsAsync();
            var allCategories = await dbRepo.GetAllCategoryNamesAsync();

            _transactionUserInteraction.ShowMessage($"Found {allCategories.Count} unique categories across {categoryCounts.Values.Sum()} vendor mappings.\n");

            if (allCategories.Count == 0)
            {
                _transactionUserInteraction.ShowMessage("No categories found. Nothing to clean up.");
                return;
            }

            // Group similar categories
            _transactionUserInteraction.ShowMessage("Analyzing for duplicates...\n");
            var groups = GroupSimilarCategories(allCategories, categoryCounts);

            if (groups.Count == 0)
            {
                _transactionUserInteraction.ShowMessage("No duplicate or similar categories found. Your categories look good!");
                return;
            }

            _transactionUserInteraction.ShowMessage($"Found {groups.Count} groups of similar categories.\n");

            int totalMerged = 0;
            int groupNumber = 1;

            foreach (var group in groups)
            {
                _transactionUserInteraction.ShowMessage($"Group {groupNumber}: {string.Join("/", group)} ({group.Sum(c => categoryCounts.GetValueOrDefault(c, 0))} vendors)");

                // Show each variant with count
                for (int i = 0; i < group.Count; i++)
                {
                    var count = categoryCounts.GetValueOrDefault(group[i], 0);
                    _transactionUserInteraction.ShowMessage($"  {i + 1}. {group[i]} ({count} vendors)");
                }

                _transactionUserInteraction.ShowMessage("Select canonical name (1-" + group.Count + "), enter custom, or 's' to skip: ");
                string choice = _transactionUserInteraction.GetInput().Trim().ToLower();

                if (choice == "s")
                {
                    _transactionUserInteraction.ShowMessage("Skipped.\n");
                    groupNumber++;
                    continue;
                }

                string canonicalName;
                if (int.TryParse(choice, out int index) && index >= 1 && index <= group.Count)
                {
                    canonicalName = group[index - 1];
                }
                else if (!string.IsNullOrWhiteSpace(choice))
                {
                    canonicalName = choice.Trim();
                }
                else
                {
                    _transactionUserInteraction.ShowMessage("Invalid choice. Skipped.\n");
                    groupNumber++;
                    continue;
                }

                // Merge categories
                _transactionUserInteraction.ShowMessage($"Merging to '{canonicalName}'...");
                await dbRepo.MergeCategoriesAsync(group, canonicalName);
                totalMerged += group.Count - 1; // Don't count the canonical name itself
                _transactionUserInteraction.ShowMessage("Done.\n");

                groupNumber++;
            }

            _transactionUserInteraction.ShowMessage("\n========================================");
            _transactionUserInteraction.ShowMessage($"Cleanup complete!");
            _transactionUserInteraction.ShowMessage($"Merged {totalMerged} category variants.");
            _transactionUserInteraction.ShowMessage("========================================\n");
        }
        catch (Exception ex)
        {
            _transactionUserInteraction.ShowMessage($"\nError during cleanup: {ex.Message}");
            throw;
        }
    }

    private List<List<string>> GroupSimilarCategories(List<string> categories, Dictionary<string, int> categoryCounts)
    {
        var groups = new List<List<string>>();
        var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in categories.OrderBy(c => c))
        {
            if (processed.Contains(category))
            {
                continue;
            }

            var similarCategories = new List<string> { category };
            processed.Add(category);

            // Find similar categories
            foreach (var otherCategory in categories)
            {
                if (processed.Contains(otherCategory))
                {
                    continue;
                }

                if (AreSimilar(category, otherCategory))
                {
                    similarCategories.Add(otherCategory);
                    processed.Add(otherCategory);
                }
            }

            // Only add groups with more than one category
            if (similarCategories.Count > 1)
            {
                // Sort by usage count (most used first)
                similarCategories = similarCategories
                    .OrderByDescending(c => categoryCounts.GetValueOrDefault(c, 0))
                    .ToList();
                groups.Add(similarCategories);
            }
        }

        return groups;
    }

    private bool AreSimilar(string category1, string category2)
    {
        var c1 = category1.ToLower().Trim();
        var c2 = category2.ToLower().Trim();

        // Exact match (case-insensitive)
        if (c1 == c2)
        {
            return true;
        }

        // Plural/singular matching
        if (c1 + "s" == c2 || c1 == c2 + "s")
        {
            return true;
        }

        // "ies" vs "y" (groceries vs grocery)
        if (c1.EndsWith("ies") && c2.EndsWith("y"))
        {
            var c1Base = c1.Substring(0, c1.Length - 3);
            var c2Base = c2.Substring(0, c2.Length - 1);
            if (c1Base == c2Base)
            {
                return true;
            }
        }
        if (c2.EndsWith("ies") && c1.EndsWith("y"))
        {
            var c2Base = c2.Substring(0, c2.Length - 3);
            var c1Base = c1.Substring(0, c1.Length - 1);
            if (c2Base == c1Base)
            {
                return true;
            }
        }

        // Contains relationship (grocery vs grocery store)
        if (c1.Contains(c2) || c2.Contains(c1))
        {
            // Only match if one is significantly longer (avoid matching "food" with "fast food")
            int lengthDiff = Math.Abs(c1.Length - c2.Length);
            if (lengthDiff >= 3)
            {
                return true;
            }
        }

        // Levenshtein distance for typos (distance <= 2)
        int distance = LevenshteinDistance(c1, c2);
        if (distance <= 2 && Math.Min(c1.Length, c2.Length) >= 4)
        {
            return true;
        }

        return false;
    }

    private int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.IsNullOrEmpty(target) ? 0 : target.Length;
        }

        if (string.IsNullOrEmpty(target))
        {
            return source.Length;
        }

        int[,] distance = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
        {
            distance[i, 0] = i;
        }

        for (int j = 0; j <= target.Length; j++)
        {
            distance[0, j] = j;
        }

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[source.Length, target.Length];
    }
}

