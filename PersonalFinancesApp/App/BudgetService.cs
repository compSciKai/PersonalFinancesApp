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
        if (existing is not null && existing.Id != profile.Id)
        {
            _transactionUserInteraction.ShowMessage($"Profile with name {profile.Name} already exists.");
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
                    budgets[newCategory] = amount;
                    _transactionUserInteraction.ShowMessage($"\n{newCategory} set to ${amount.ToString("0.00")}");
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

            foreach (var profile in jsonProfiles)
            {
                var existing = existingProfiles.FirstOrDefault(p => p.Name == profile.Name);

                if (existing == null)
                {
                    // Profile doesn't exist in database, migrate it
                    await _databaseRepository.SaveBudgetProfileAsync(profile);
                    migratedCount++;
                    _transactionUserInteraction.ShowMessage($"  ✓ Migrated profile: {profile.Name}");
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
        }
        catch (Exception ex)
        {
            _transactionUserInteraction.ShowMessage($"Error during migration: {ex.Message}");
            throw;
        }
    }
}
