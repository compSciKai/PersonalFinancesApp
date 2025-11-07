using System.Numerics;
using PersonalFinances.Repositories;
using PersonalFinances.Models;

namespace PersonalFinances.App;

public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _databaseRepository;
    private readonly IBudgetRepository? _jsonRepository;
    private readonly ITransactionsUserInteraction _transactionUserInteraction;

    public BudgetService(
        IBudgetRepository databaseRepository,
        ITransactionsUserInteraction transactionUserInteraction,
        IBudgetRepository? jsonRepository = null)
    {
        _databaseRepository = databaseRepository;
        _jsonRepository = jsonRepository;
        _transactionUserInteraction = transactionUserInteraction;
    }
    public async Task<List<BudgetProfile>> LoadProfilesAsync()
    {
        return await _databaseRepository.LoadBudgetProfilesAsync();
    }

    public async Task<BudgetProfile?> GetProfileAsync(string profileName)
    {
        return await _databaseRepository.GetProfileByNameAsync(profileName);
    }

    public async Task<BudgetProfile?> GetProfileByIdAsync(int id)
    {
        return await _databaseRepository.GetProfileByIdAsync(id);
    }

    public async Task StoreProfileAsync(BudgetProfile profile)
    {
        var existing = await GetProfileAsync(profile.Name);
        // Reject duplicate names:
        // - For new profiles (Id == 0): reject if any profile with that name exists
        // - For existing profiles (Id != 0): reject if a different profile with that name exists
        if (existing is not null)
        {
            // If saving a new profile (Id == 0) or updating a different profile
            if (profile.Id == 0 || existing.Id != profile.Id)
            {
                _transactionUserInteraction.ShowMessage($"Profile with name {profile.Name} already exists.");
                return;
            }
        }

        // Validate categories for duplicates
        var (isValid, duplicateCategories) = profile.ValidateCategories();
        if (!isValid)
        {
            _transactionUserInteraction.ShowMessage(
                $"Error: Cannot save profile. Duplicate categories found: {string.Join(", ", duplicateCategories)}");
            return;
        }

        // Save to database
        await _databaseRepository.SaveBudgetProfileAsync(profile);

        // Also save to JSON as backup if repository is provided
        if (_jsonRepository != null)
        {
            try
            {
                await _jsonRepository.SaveBudgetProfileAsync(profile);
            }
            catch (Exception ex)
            {
                _transactionUserInteraction.ShowMessage($"Warning: Could not save to JSON backup: {ex.Message}");
            }
        }
    }

    private async Task<List<string>> GetProfileNamesAsync()
    {
        var profiles = await LoadProfilesAsync();
        return profiles.Select(p => p.Name).ToList();
    }

    public async Task<BudgetProfile?> GetActiveProfileAsync()
    {
        var profileNames = await GetProfileNamesAsync();
        string? profileChoice = null;
        if (profileNames.Count > 0)
        {
            profileChoice = _transactionUserInteraction.PromptForProfileChoice(profileNames);
        }

        if (profileChoice is not null)
        {
            return await GetProfileAsync(profileChoice);
        }

        return null;
    }

    public async Task<BudgetProfile> CreateNewProfileAsync()
    {
        _transactionUserInteraction.ShowMessage("What is the name for this profile?");
        string profileName = _transactionUserInteraction.GetInput();

        _transactionUserInteraction.ShowMessage("Who is this profile for?");
        string userName = _transactionUserInteraction.GetInput();

        _transactionUserInteraction.ShowMessage("Set a description for this profile, or press enter.\n");
        string description = _transactionUserInteraction.GetInput();

        bool isIncorrectIncomeValue = true;
        double income = 0;
        while (isIncorrectIncomeValue)
        {
            _transactionUserInteraction.ShowMessage("What is the monthly take-home income for this profile?\n");
            string incomeString = _transactionUserInteraction.GetInput();

            isIncorrectIncomeValue = !double.TryParse(incomeString, out income);
        }

        Dictionary<string, double> budgets = new Dictionary<string, double>();
        bool isMoreCategories = true;
        while (isMoreCategories)
        {
            _transactionUserInteraction.ShowMessage("Add a new budget category."
            + " press enter when finished.\n");
            string newCategory = _transactionUserInteraction.GetInput();

            if (newCategory != "")
            {
                _transactionUserInteraction.ShowMessage("What is the limit for this budget category?\n");
                string stringAmount = _transactionUserInteraction.GetInput();

                double amount;
                if (double.TryParse(stringAmount, out amount))
                {
                    // Check if category already exists (case-insensitive)
                    var existingKey = budgets.Keys.FirstOrDefault(k =>
                        k.Equals(newCategory, StringComparison.OrdinalIgnoreCase));

                    if (existingKey != null)
                    {
                        var oldAmount = budgets[existingKey];
                        budgets[existingKey] = amount;
                        _transactionUserInteraction.ShowMessage(
                            $"\nUpdating '{existingKey}' from ${oldAmount.ToString("0.00")} to ${amount.ToString("0.00")}");
                    }
                    else
                    {
                        budgets[newCategory] = amount;
                        _transactionUserInteraction.ShowMessage($"\n{newCategory} set to ${amount.ToString("0.00")}");
                    }
                }
            }
            else
            {
                isMoreCategories = false;
            }
        }

        BudgetProfile newBudgetProfile = new BudgetProfile(profileName, budgets, income, userName, description);
        await StoreProfileAsync(newBudgetProfile);

        return newBudgetProfile;
    }

    public double GetBudgetTotal(BudgetProfile profile)
    {
        List<double> budgetCategoryTotals = profile.BudgetCategories.Values.ToList();

        double sum = 0;
        foreach (var total in budgetCategoryTotals)
        {
            sum += total;
        }

        return sum;
    }

    public async Task MigrateProfilesToDatabaseAsync()
    {
        if (_jsonRepository == null)
        {
            _transactionUserInteraction.ShowMessage("No JSON repository configured for migration.");
            return;
        }

        _transactionUserInteraction.ShowMessage("Starting migration of profiles from JSON to database...");

        try
        {
            // Load profiles from JSON
            var jsonProfiles = await _jsonRepository.LoadBudgetProfilesAsync();

            if (jsonProfiles.Count == 0)
            {
                _transactionUserInteraction.ShowMessage("No profiles found in JSON file to migrate.");
                return;
            }

            _transactionUserInteraction.ShowMessage($"Found {jsonProfiles.Count} profiles to migrate.");

            // Check which profiles already exist in database
            var existingProfiles = await _databaseRepository.LoadBudgetProfilesAsync();
            int migratedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            foreach (var profile in jsonProfiles)
            {
                var existing = existingProfiles.FirstOrDefault(p => p.Name == profile.Name);

                if (existing == null)
                {
                    // Validate profile before migration
                    var (isValid, duplicateCategories) = profile.ValidateCategories();
                    if (!isValid)
                    {
                        errorCount++;
                        _transactionUserInteraction.ShowMessage(
                            $"  ✗ Error: Profile '{profile.Name}' has duplicate categories: {string.Join(", ", duplicateCategories)}");
                        continue;
                    }

                    try
                    {
                        // Profile doesn't exist in database, migrate it
                        await _databaseRepository.SaveBudgetProfileAsync(profile);
                        migratedCount++;
                        _transactionUserInteraction.ShowMessage($"  ✓ Migrated profile: {profile.Name}");
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _transactionUserInteraction.ShowMessage(
                            $"  ✗ Error migrating profile '{profile.Name}': {ex.Message}");
                    }
                }
                else
                {
                    skippedCount++;
                    _transactionUserInteraction.ShowMessage($"  - Skipped profile (already exists): {profile.Name}");
                }
            }

            _transactionUserInteraction.ShowMessage($"\nMigration complete!");
            _transactionUserInteraction.ShowMessage($"  Migrated: {migratedCount}");
            _transactionUserInteraction.ShowMessage($"  Skipped: {skippedCount}");
            if (errorCount > 0)
            {
                _transactionUserInteraction.ShowMessage($"  Errors: {errorCount}");
            }
        }
        catch (Exception ex)
        {
            _transactionUserInteraction.ShowMessage($"Error during migration: {ex.Message}");
            throw;
        }
    }

    public async Task<BudgetProfile?> EditProfileAsync(BudgetProfile profile)
    {
        // Create a working copy to track changes
        var originalProfile = new BudgetProfile
        {
            Id = profile.Id,
            Name = profile.Name,
            UserName = profile.UserName,
            Description = profile.Description,
            Income = profile.Income,
            Categories = profile.Categories.Select(c => new BudgetCategory
            {
                CategoryName = c.CategoryName,
                BudgetAmount = c.BudgetAmount
            }).ToList()
        };

        var workingProfile = profile; // Edit this one
        var changes = new List<string>(); // Track what changed

        bool editing = true;
        while (editing)
        {
            _transactionUserInteraction.ShowMessage("\n=== Edit Profile ===");
            _transactionUserInteraction.ShowMessage($"1. Name ({workingProfile.Name})");
            _transactionUserInteraction.ShowMessage($"2. Username ({workingProfile.UserName})");
            _transactionUserInteraction.ShowMessage($"3. Description ({workingProfile.Description})");
            _transactionUserInteraction.ShowMessage($"4. Income (${workingProfile.Income.ToString("0.00")})");
            _transactionUserInteraction.ShowMessage($"5. Categories ({workingProfile.Categories.Count} items)");
            _transactionUserInteraction.ShowMessage("6. Save changes");
            _transactionUserInteraction.ShowMessage("7. Cancel (discard changes)\n");

            _transactionUserInteraction.ShowMessage("Select option (1-7): ");
            string choice = _transactionUserInteraction.GetInput().Trim();

            switch (choice)
            {
                case "1":
                    EditProfileName(workingProfile, originalProfile, changes);
                    break;
                case "2":
                    EditUserName(workingProfile, originalProfile, changes);
                    break;
                case "3":
                    EditDescription(workingProfile, originalProfile, changes);
                    break;
                case "4":
                    EditIncome(workingProfile, originalProfile, changes);
                    break;
                case "5":
                    EditCategories(workingProfile, originalProfile, changes);
                    break;
                case "6":
                    // Show summary and confirm
                    if (await ConfirmAndSaveChangesAsync(workingProfile, changes))
                    {
                        return workingProfile; // Saved successfully
                    }
                    break;
                case "7":
                    _transactionUserInteraction.ShowMessage("Changes discarded.");
                    return null; // Cancelled
                default:
                    _transactionUserInteraction.ShowMessage($"Invalid option '{choice}'. Please select 1-7.\n");
                    break;
            }
        }

        return null;
    }

    private void EditProfileName(BudgetProfile workingProfile, BudgetProfile originalProfile, List<string> changes)
    {
        _transactionUserInteraction.ShowMessage($"\nCurrent name: {workingProfile.Name}");
        _transactionUserInteraction.ShowMessage("Enter new name (or press Enter to keep): ");
        string newName = _transactionUserInteraction.GetInput().Trim();

        if (!string.IsNullOrEmpty(newName) && newName != workingProfile.Name)
        {
            changes.Add($"Name: '{workingProfile.Name}' → '{newName}'");
            workingProfile.Name = newName;
            _transactionUserInteraction.ShowMessage($"Name updated to: {newName}\n");
        }
        else
        {
            _transactionUserInteraction.ShowMessage("Name unchanged.\n");
        }
    }

    private void EditUserName(BudgetProfile workingProfile, BudgetProfile originalProfile, List<string> changes)
    {
        _transactionUserInteraction.ShowMessage($"\nCurrent username: {workingProfile.UserName}");
        _transactionUserInteraction.ShowMessage("Enter new username (or press Enter to keep): ");
        string newUserName = _transactionUserInteraction.GetInput().Trim();

        if (!string.IsNullOrEmpty(newUserName) && newUserName != workingProfile.UserName)
        {
            changes.Add($"Username: '{workingProfile.UserName}' → '{newUserName}'");
            workingProfile.UserName = newUserName;
            _transactionUserInteraction.ShowMessage($"Username updated to: {newUserName}\n");
        }
        else
        {
            _transactionUserInteraction.ShowMessage("Username unchanged.\n");
        }
    }

    private void EditDescription(BudgetProfile workingProfile, BudgetProfile originalProfile, List<string> changes)
    {
        _transactionUserInteraction.ShowMessage($"\nCurrent description: {workingProfile.Description}");
        _transactionUserInteraction.ShowMessage("Enter new description (or press Enter to keep): ");
        string newDescription = _transactionUserInteraction.GetInput().Trim();

        if (newDescription != workingProfile.Description) // Allow setting to empty
        {
            changes.Add($"Description: '{workingProfile.Description}' → '{newDescription}'");
            workingProfile.Description = newDescription;
            _transactionUserInteraction.ShowMessage($"Description updated to: {newDescription}\n");
        }
        else
        {
            _transactionUserInteraction.ShowMessage("Description unchanged.\n");
        }
    }

    private void EditIncome(BudgetProfile workingProfile, BudgetProfile originalProfile, List<string> changes)
    {
        _transactionUserInteraction.ShowMessage($"\nCurrent income: ${workingProfile.Income.ToString("0.00")}");
        _transactionUserInteraction.ShowMessage("Enter new income (or press Enter to keep): ");
        string incomeInput = _transactionUserInteraction.GetInput().Trim();

        if (!string.IsNullOrEmpty(incomeInput))
        {
            if (double.TryParse(incomeInput, out double newIncome))
            {
                if (newIncome != workingProfile.Income)
                {
                    changes.Add($"Income: ${workingProfile.Income.ToString("0.00")} → ${newIncome.ToString("0.00")}");
                    workingProfile.Income = newIncome;
                    _transactionUserInteraction.ShowMessage($"Income updated to: ${newIncome.ToString("0.00")}\n");
                }
                else
                {
                    _transactionUserInteraction.ShowMessage("Income unchanged.\n");
                }
            }
            else
            {
                _transactionUserInteraction.ShowMessage($"Invalid amount '{incomeInput}'. Income unchanged.\n");
            }
        }
        else
        {
            _transactionUserInteraction.ShowMessage("Income unchanged.\n");
        }
    }

    private void EditCategories(BudgetProfile workingProfile, BudgetProfile originalProfile, List<string> changes)
    {
        bool editingCategories = true;
        var categoryChanges = new List<string>();

        while (editingCategories)
        {
            _transactionUserInteraction.ShowMessage("\n=== Edit Categories ===");

            // Display budget summary
            double categoryTotal = workingProfile.Categories.Sum(c => c.BudgetAmount);
            double remaining = workingProfile.Income - categoryTotal;
            _transactionUserInteraction.ShowMessage($"Income: ${workingProfile.Income:0.00} | Allocated: ${categoryTotal:0.00} | Remaining: ${remaining:0.00}\n");

            if (workingProfile.Categories.Count == 0)
            {
                _transactionUserInteraction.ShowMessage("No categories. Press 'a' to add categories.\n");
            }
            else
            {
                int index = 1;
                foreach (var category in workingProfile.Categories.OrderBy(c => c.CategoryName))
                {
                    _transactionUserInteraction.ShowMessage($"{index}. {category.CategoryName}: ${category.BudgetAmount.ToString("0.00")}");
                    index++;
                }
                _transactionUserInteraction.ShowMessage("");
            }

            _transactionUserInteraction.ShowMessage("Enter number to edit, 'a' to add, 'r' to remove, or Enter when done: ");
            string choice = _transactionUserInteraction.GetInput().Trim().ToLower();

            if (string.IsNullOrEmpty(choice))
            {
                // Done editing categories
                editingCategories = false;

                // Add category changes to main changes list
                if (categoryChanges.Count > 0)
                {
                    changes.Add($"Categories: {categoryChanges.Count} changes");
                    changes.AddRange(categoryChanges.Select(c => $"  - {c}"));
                }
            }
            else if (choice == "a")
            {
                // Add new category
                _transactionUserInteraction.ShowMessage("Enter category name: ");
                string categoryName = _transactionUserInteraction.GetInput().Trim();

                if (string.IsNullOrEmpty(categoryName))
                {
                    _transactionUserInteraction.ShowMessage("Category name cannot be empty.\n");
                    continue;
                }

                // Check for duplicates (case-insensitive)
                var existingCategory = workingProfile.Categories.FirstOrDefault(c =>
                    c.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

                if (existingCategory != null)
                {
                    _transactionUserInteraction.ShowMessage($"Category '{categoryName}' already exists with amount ${existingCategory.BudgetAmount.ToString("0.00")}.\n");
                    continue;
                }

                _transactionUserInteraction.ShowMessage("Enter budget amount: ");
                string amountInput = _transactionUserInteraction.GetInput().Trim();

                if (double.TryParse(amountInput, out double amount))
                {
                    // Validate against remaining budget
                    double currentTotal = workingProfile.Categories.Sum(c => c.BudgetAmount);
                    double remainingBudget = workingProfile.Income - currentTotal;

                    if (amount > remainingBudget)
                    {
                        _transactionUserInteraction.ShowMessage($"Cannot add category. Amount ${amount:0.00} exceeds remaining budget of ${remainingBudget:0.00}.\n");
                        continue;
                    }

                    workingProfile.Categories.Add(new BudgetCategory
                    {
                        CategoryName = categoryName,
                        BudgetAmount = amount
                    });
                    categoryChanges.Add($"Added '{categoryName}': ${amount.ToString("0.00")}");
                    _transactionUserInteraction.ShowMessage($"Category '{categoryName}' added with amount ${amount.ToString("0.00")}\n");
                }
                else
                {
                    _transactionUserInteraction.ShowMessage($"Invalid amount '{amountInput}'.\n");
                }
            }
            else if (choice == "r")
            {
                // Remove category
                if (workingProfile.Categories.Count == 0)
                {
                    _transactionUserInteraction.ShowMessage("No categories to remove.\n");
                    continue;
                }

                _transactionUserInteraction.ShowMessage("Enter number of category to remove: ");
                string numberInput = _transactionUserInteraction.GetInput().Trim();

                if (int.TryParse(numberInput, out int categoryNumber) &&
                    categoryNumber >= 1 && categoryNumber <= workingProfile.Categories.Count)
                {
                    var categoryToRemove = workingProfile.Categories
                        .OrderBy(c => c.CategoryName)
                        .ElementAt(categoryNumber - 1);

                    workingProfile.Categories.Remove(categoryToRemove);
                    categoryChanges.Add($"Removed '{categoryToRemove.CategoryName}': ${categoryToRemove.BudgetAmount.ToString("0.00")}");
                    _transactionUserInteraction.ShowMessage($"Category '{categoryToRemove.CategoryName}' removed.\n");
                }
                else
                {
                    _transactionUserInteraction.ShowMessage($"Invalid category number '{numberInput}'.\n");
                }
            }
            else if (int.TryParse(choice, out int editNumber) &&
                     editNumber >= 1 && editNumber <= workingProfile.Categories.Count)
            {
                // Edit existing category amount
                var categoryToEdit = workingProfile.Categories
                    .OrderBy(c => c.CategoryName)
                    .ElementAt(editNumber - 1);

                _transactionUserInteraction.ShowMessage($"\nCurrent amount for '{categoryToEdit.CategoryName}': ${categoryToEdit.BudgetAmount.ToString("0.00")}");
                _transactionUserInteraction.ShowMessage("Enter new amount (or press Enter to keep): ");
                string amountInput = _transactionUserInteraction.GetInput().Trim();

                if (!string.IsNullOrEmpty(amountInput))
                {
                    if (double.TryParse(amountInput, out double newAmount))
                    {
                        if (newAmount != categoryToEdit.BudgetAmount)
                        {
                            // Validate against remaining budget
                            double totalWithoutThisCategory = workingProfile.Categories
                                .Where(c => c != categoryToEdit)
                                .Sum(c => c.BudgetAmount);

                            if (totalWithoutThisCategory + newAmount > workingProfile.Income)
                            {
                                double maxAllowed = workingProfile.Income - totalWithoutThisCategory;
                                _transactionUserInteraction.ShowMessage($"Cannot update amount. New total would exceed income. Maximum allowed: ${maxAllowed:0.00}\n");
                                continue;
                            }

                            categoryChanges.Add($"'{categoryToEdit.CategoryName}': ${categoryToEdit.BudgetAmount.ToString("0.00")} → ${newAmount.ToString("0.00")}");
                            categoryToEdit.BudgetAmount = newAmount;
                            _transactionUserInteraction.ShowMessage($"Amount updated to ${newAmount.ToString("0.00")}\n");
                        }
                    }
                    else
                    {
                        _transactionUserInteraction.ShowMessage($"Invalid amount '{amountInput}'.\n");
                    }
                }
            }
            else
            {
                _transactionUserInteraction.ShowMessage($"Invalid option '{choice}'.\n");
            }
        }
    }

    private async Task<bool> ConfirmAndSaveChangesAsync(BudgetProfile profile, List<string> changes)
    {
        if (changes.Count == 0)
        {
            _transactionUserInteraction.ShowMessage("\nNo changes were made.\n");
            return false;
        }

        _transactionUserInteraction.ShowMessage("\n" + new string('=', 50));
        _transactionUserInteraction.ShowMessage("=== Changes Summary ===");
        foreach (var change in changes)
        {
            _transactionUserInteraction.ShowMessage(change);
        }
        _transactionUserInteraction.ShowMessage(new string('=', 50));

        _transactionUserInteraction.ShowMessage("\nSave these changes? (y/n): ");
        string confirm = _transactionUserInteraction.GetInput().Trim().ToLower();

        if (confirm == "y" || confirm == "yes")
        {
            try
            {
                await StoreProfileAsync(profile);
                _transactionUserInteraction.ShowMessage("\nProfile updated successfully!\n");
                return true;
            }
            catch (Exception ex)
            {
                _transactionUserInteraction.ShowMessage($"\nError saving profile: {ex.Message}\n");
                return false;
            }
        }
        else
        {
            _transactionUserInteraction.ShowMessage("\nChanges not saved.\n");
            return false;
        }
    }
}
