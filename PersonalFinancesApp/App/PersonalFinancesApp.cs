using PersonalFinances.Repositories;
using PersonalFinances.Models;
using System.Text.Json;

namespace PersonalFinances.App;
class PersonalFinancesApp
{
    private const string ConfigFilePath = "appconfig.json";
    private readonly IFileTransactionRepository<RBCTransaction> _rbcCsvRepository;
    private readonly IFileTransactionRepository<AmexTransaction> _amexCsvRepository;
    private readonly IFileTransactionRepository<PCFinancialTransaction> _pcCsvRepository;
    private readonly ITransactionRepository<RBCTransaction> _rbcSqlRepository;
    private readonly ITransactionRepository<AmexTransaction> _amexSqlRepository;
    private readonly ITransactionRepository<PCFinancialTransaction> _pcSqlRepository;
    private readonly ITransactionsUserInteraction _transactionUserInteraction;
    private readonly IVendorsService _vendorsService;
    private readonly ICategoriesService _categoriesService;
    private readonly IBudgetService _budgetService;

    public PersonalFinancesApp(
        IFileTransactionRepository<RBCTransaction> rbcCsvRepository,
        IFileTransactionRepository<AmexTransaction> amexCsvRepository,
        IFileTransactionRepository<PCFinancialTransaction> pcCsvRepository,
        ITransactionRepository<RBCTransaction> rbcSqlRepository,
        ITransactionRepository<AmexTransaction> amexSqlRepository,
        ITransactionRepository<PCFinancialTransaction> pcSqlRepository,
        ITransactionsUserInteraction transactionUserInteraction,
        IVendorsService vendorsService,
        ICategoriesService categoriesService,
        IBudgetService budgetService
        )
    {
        _rbcCsvRepository = rbcCsvRepository;
        _amexCsvRepository = amexCsvRepository;
        _pcCsvRepository = pcCsvRepository;
        _rbcSqlRepository = rbcSqlRepository;
        _amexSqlRepository = amexSqlRepository;
        _pcSqlRepository = pcSqlRepository;
        _transactionUserInteraction = transactionUserInteraction;
        _vendorsService = vendorsService;
        _categoriesService = categoriesService;
        _budgetService = budgetService;
    }

    private async Task<string?> LoadLastUsedProfileAsync()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = await File.ReadAllTextAsync(ConfigFilePath);
                var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return config?.ContainsKey("LastUsedProfile") == true ? config["LastUsedProfile"] : null;
            }
        }
        catch (Exception ex)
        {
            _transactionUserInteraction.ShowMessage($"Warning: Could not load last used profile: {ex.Message}");
        }
        return null;
    }

    private async Task SaveLastUsedProfileAsync(string profileName)
    {
        try
        {
            var config = new Dictionary<string, string>
            {
                ["LastUsedProfile"] = profileName,
                ["LastUpdated"] = DateTime.UtcNow.ToString("o")
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            _transactionUserInteraction.ShowMessage($"Warning: Could not save last used profile: {ex.Message}");
        }
    }

    public async Task RunAsync(Dictionary<string, Type> transactionsDictionary, TransactionFilterService.TransactionRange? transactionFilterString, string? currentProfile)
    {
        // load data from sources
        Console.WriteLine("Finances App Initialized\n");
        List<string> categories = _categoriesService.GetAllCategories();

        BudgetProfile? profile = null;
        bool createNewProfile = false;

        // Load last used profile if no current profile specified
        if (string.IsNullOrEmpty(currentProfile))
        {
            var lastUsedProfile = await LoadLastUsedProfileAsync();
            if (!string.IsNullOrEmpty(lastUsedProfile))
            {
                currentProfile = lastUsedProfile;
                _transactionUserInteraction.ShowMessage($"Last used profile: {currentProfile}");
            }
        }

        // If a profile name is specified, ask user what they want to do
        if (!string.IsNullOrEmpty(currentProfile))
        {
            _transactionUserInteraction.ShowMessage($"Profile '{currentProfile}' specified.");
            _transactionUserInteraction.ShowMessage("What would you like to do?");
            _transactionUserInteraction.ShowMessage("1. Load existing profile (default)");
            _transactionUserInteraction.ShowMessage("2. Create new profile");
            _transactionUserInteraction.ShowMessage("3. Select from available profiles");
            _transactionUserInteraction.ShowMessage("4. Quick launch (press Enter to continue)\n");

            string choice = _transactionUserInteraction.GetInput().Trim();

            // Input validation
            if (!new[] { "1", "2", "3", "4", "" }.Contains(choice))
            {
                _transactionUserInteraction.ShowMessage($"Invalid choice '{choice}'. Using default (1).\n");
                choice = "1";
            }

            if (choice == "2")
            {
                createNewProfile = true;
            }
            else if (choice == "3")
            {
                profile = await _budgetService.GetActiveProfileAsync();
            }
            else if (choice == "4" || choice == "")
            {
                // Quick launch - load profile directly without prompting
                profile = await _budgetService.GetProfileAsync(currentProfile);
                if (profile is null)
                {
                    _transactionUserInteraction.ShowMessage($"Profile '{currentProfile}' not found.");
                    profile = await _budgetService.GetActiveProfileAsync();
                }
                else
                {
                    _transactionUserInteraction.ShowMessage($"Quick launching with profile '{currentProfile}'...\n");
                }
            }
            else // Option 1 - load existing with confirmation
            {
                profile = await _budgetService.GetProfileAsync(currentProfile);
                if (profile is null)
                {
                    _transactionUserInteraction.ShowMessage($"Profile '{currentProfile}' not found.");
                    profile = await _budgetService.GetActiveProfileAsync();
                }
                else
                {
                    // Show profile details before confirming
                    _transactionUserInteraction.ShowMessage("\n=== Profile Details ===");
                    _transactionUserInteraction.ShowMessage(profile.ToString());
                    _transactionUserInteraction.ShowMessage("=======================\n");

                    _transactionUserInteraction.ShowMessage("Press Enter to continue or 'n' to choose a different profile: ");
                    string confirm = _transactionUserInteraction.GetInput().Trim().ToLower();

                    if (confirm == "n")
                    {
                        _transactionUserInteraction.ShowMessage("");
                        profile = await _budgetService.GetActiveProfileAsync();
                    }
                }
            }
        }

        // Create new profile if requested or if no profile loaded yet
        if (createNewProfile)
        {
            profile = await _budgetService.CreateNewProfileAsync();
        }
        else if (profile is null)
        {
            // Try to get active profile or create first one
            profile = await _budgetService.GetActiveProfileAsync();
            if (profile is null)
            {
                _transactionUserInteraction.ShowMessage("No budget profiles found. Creating first profile...");
                profile = await _budgetService.CreateNewProfileAsync();
            }
        }

        // Save the selected profile as last used
        await SaveLastUsedProfileAsync(profile.Name);

        double budgetTotal = _budgetService.GetBudgetTotal(profile);

        // show profile info

        // TODO: output profile. Ask user if it is okay or would like to edit
        _transactionUserInteraction.ShowMessage($"Budget profile set to:\n");
        _transactionUserInteraction.ShowMessage(profile.ToString());
        _transactionUserInteraction.ShowMessage($"\nBudget Total: ${budgetTotal.ToString("0.00")}");

        _transactionUserInteraction.ShowMessage("\nPress 'q' to quit, or enter to continue...");
        var input = _transactionUserInteraction.GetInput();
        if (input == "q")
        {
            _transactionUserInteraction.Exit();
        }

        // Get new transactions from CSV repository
        foreach (var transactionEntry in transactionsDictionary)
        {
            if (string.IsNullOrEmpty(transactionEntry.Key))
            {
                continue; // Skip empty keys
            }
            else if (transactionEntry.Value == typeof(RBCTransaction))
            {
                var transactions = await _rbcCsvRepository.LoadFromFileAsync(transactionEntry.Key);
                await _rbcSqlRepository.SaveAsync(transactions);
            }
            else if (transactionEntry.Value == typeof(AmexTransaction))
            {
                var transactions = await _amexCsvRepository.LoadFromFileAsync(transactionEntry.Key);
                await _amexSqlRepository.SaveAsync(transactions);
            }
            else if (transactionEntry.Value == typeof(PCFinancialTransaction))
            {
                var transactions = await _pcCsvRepository.LoadFromFileAsync(transactionEntry.Key);
                await _pcSqlRepository.SaveAsync(transactions);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported transaction type: {transactionEntry.Value}");
            }
        }

        // fetch all transactions
        var rbcTransactions = await _rbcSqlRepository.GetAllAsync();
        var amexTransactions = await _amexSqlRepository.GetAllAsync();
        var pcTransactions = await _pcSqlRepository.GetAllAsync();
        
        var allTransactions = new List<Transaction>();
        allTransactions.AddRange(rbcTransactions);
        allTransactions.AddRange(amexTransactions);
        allTransactions.AddRange(pcTransactions);

        // process transactions if missing fields

        // construct list of transactions and handler dictionary, iterate over
        List<Transaction> transactionsWithVendors = _vendorsService.AddVendorsToTransactions(allTransactions);
        List<Transaction> transactionsWithCategories = _categoriesService.AddCategoriesToTransactions(transactionsWithVendors);
        List<Transaction> filteredTransactions = TransactionFilterService.GetTransactionsInRange(transactionsWithCategories, transactionFilterString);

        if (profile.UserName != null)
        {
            filteredTransactions = TransactionFilterService.GetTransactionsForUser(filteredTransactions, profile.UserName);
        }

        filteredTransactions = _categoriesService.OverrideCategories(filteredTransactions, "Restaurant", "Entertainment");

        List<Transaction> spendingTransactions = TransactionFilterService.GetSpendingTransactions(filteredTransactions);

        string rangeType = TransactionFilterService.GetHumanReadableTransactionRange(transactionFilterString);
        string tableName = rangeType is not null ? $"{rangeType} Transactions" : "Transactions";


        // output information 
        _transactionUserInteraction.OutputTransactions(spendingTransactions, tableName, null);

        foreach (string category in categories)
        {
            List<Transaction> categorizedTransactions = filteredTransactions.Where(transaction => transaction.Category == category).ToList();

            if (profile.BudgetCategories.Any(c => c.Key.ToLower() == category.ToLower()))
            {
                _transactionUserInteraction.OutputTransactions(categorizedTransactions, category, profile);
            }
        }

        // Output Budget Vs Actual Spending Totals
        _transactionUserInteraction.OutputBudgetVsActual(filteredTransactions, profile);


        /* TODO:
        - [ ] Fix transfers
        - [ ] Fix Create table for other transactions
        - [ ] create method to find specific trnansactions via name and amount to categorize as, rent, student loan, etc -- take one
        - [ ] create total expense vs diff
        - [ ] aim to get caluclates to become exact
        - [ ] in budget vs actual, determine unaccounted for transactions, ie. missing student loan etc

        */
        //_transactionCsvRepository.ExportTransactions(filteredTransactions, "./export-test.csv");

    }
}