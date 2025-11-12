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
    private readonly ITransferManagementService _transferManagementService;
    private readonly ITransactionReprocessingService _reprocessingService;

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
        IBudgetService budgetService,
        ITransferManagementService transferManagementService,
        ITransactionReprocessingService reprocessingService
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
        _transferManagementService = transferManagementService;
        _reprocessingService = reprocessingService;
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

    public async Task RunAsync(Dictionary<string, Type> transactionsDictionary, TransactionFilterService.TransactionRange? transactionFilterString)
    {
        // load data from sources
        Console.WriteLine("Finances App Initialized\n");
        List<string> categories = _categoriesService.GetAllCategories();

        BudgetProfile? profile = null;
        bool createNewProfile = false;
        var currentProfile = "";

        var lastUsedProfile = await LoadLastUsedProfileAsync();
        if (!string.IsNullOrEmpty(lastUsedProfile))
        {
            currentProfile = lastUsedProfile;
            _transactionUserInteraction.ShowMessage($"Last used profile: {currentProfile}");
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
        _transactionUserInteraction.ShowMessage($"Budget profile set to:\n");
        _transactionUserInteraction.ShowMessage(profile.ToString());
        _transactionUserInteraction.ShowMessage($"\nBudget Total: ${budgetTotal.ToString("0.00")}");

        _transactionUserInteraction.ShowMessage("\nWhat would you like to do?");
        _transactionUserInteraction.ShowMessage("1. Continue to transactions (default)");
        _transactionUserInteraction.ShowMessage("2. Edit profile");
        _transactionUserInteraction.ShowMessage("3. Category cleanup");
        _transactionUserInteraction.ShowMessage("4. Reprocess untyped transactions");
        _transactionUserInteraction.ShowMessage("5. Quit\n");

        string userInput = _transactionUserInteraction.GetInput().Trim();

        // Input validation
        if (!new[] { "1", "2", "3", "4", "5", "" }.Contains(userInput))
        {
            _transactionUserInteraction.ShowMessage($"Invalid choice '{userInput}'. Using default (1).\n");
            userInput = "1";
        }

        if (userInput == "2")
        {
            // Edit loop
            bool continueEditing = true;
            while (continueEditing)
            {
                var editedProfile = await _budgetService.EditProfileAsync(profile);

                if (editedProfile != null) // User confirmed changes
                {
                    profile = editedProfile; // Update current profile reference
                    await SaveLastUsedProfileAsync(profile.Name); // Update last used if name changed

                    // Re-display updated profile
                    _transactionUserInteraction.ShowMessage("\n" + new string('=', 50));
                    _transactionUserInteraction.ShowMessage($"Budget profile set to:\n");
                    _transactionUserInteraction.ShowMessage(profile.ToString());
                    budgetTotal = _budgetService.GetBudgetTotal(profile);
                    _transactionUserInteraction.ShowMessage($"\nBudget Total: ${budgetTotal.ToString("0.00")}");
                    _transactionUserInteraction.ShowMessage(new string('=', 50) + "\n");

                    // Ask again
                    _transactionUserInteraction.ShowMessage("\nWhat would you like to do?");
                    _transactionUserInteraction.ShowMessage("1. Continue to transactions (default)");
                    _transactionUserInteraction.ShowMessage("2. Edit profile");
                    _transactionUserInteraction.ShowMessage("3. Category cleanup");
                    _transactionUserInteraction.ShowMessage("4. Reprocess untyped transactions");
                    _transactionUserInteraction.ShowMessage("5. Quit\n");

                    userInput = _transactionUserInteraction.GetInput().Trim();

                    // Input validation
                    if (!new[] { "1", "2", "3", "4", "5", "" }.Contains(userInput))
                    {
                        _transactionUserInteraction.ShowMessage($"Invalid choice '{userInput}'. Using default (1).\n");
                        userInput = "1";
                    }

                    if (userInput != "2" && userInput != "3" && userInput != "4")
                    {
                        continueEditing = false;
                    }
                }
                else // User cancelled
                {
                    continueEditing = false;
                }
            }
        }

        if (userInput == "3")
        {
            // Category cleanup
            await _categoriesService.RunCategoryCleanupAsync(profile);

            // After cleanup, show menu again
            _transactionUserInteraction.ShowMessage("\nWhat would you like to do?");
            _transactionUserInteraction.ShowMessage("1. Continue to transactions (default)");
            _transactionUserInteraction.ShowMessage("2. Edit profile");
            _transactionUserInteraction.ShowMessage("3. Category cleanup");
            _transactionUserInteraction.ShowMessage("4. Reprocess untyped transactions");
            _transactionUserInteraction.ShowMessage("5. Quit\n");

            userInput = _transactionUserInteraction.GetInput().Trim();

            // Input validation
            if (!new[] { "1", "2", "3", "4", "5", "" }.Contains(userInput))
            {
                _transactionUserInteraction.ShowMessage($"Invalid choice '{userInput}'. Using default (1).\n");
                userInput = "1";
            }

            if (userInput == "5")
            {
                _transactionUserInteraction.Exit();
            }
        }

        if (userInput == "4")
        {
            // Reprocess untyped transactions within the configured date range for current user
            var transactionsToReprocess = await _reprocessingService.GetUnprocessedTransactionsAsync(transactionFilterString, profile.UserName);

            if (transactionsToReprocess.Any())
            {
                _transactionUserInteraction.ShowMessage($"\nFound {transactionsToReprocess.Count} unprocessed transaction(s) with Type=0 in date range '{TransactionFilterService.GetHumanReadableTransactionRange(transactionFilterString)}'.");
                _transactionUserInteraction.ShowMessage("Reprocess these transactions? (y/n): ");
                var response = _transactionUserInteraction.GetInput().Trim().ToLower();

                if (response == "y")
                {
                    await _reprocessingService.ReprocessTransactionsAsync(transactionsToReprocess, profile);
                }
                else
                {
                    _transactionUserInteraction.ShowMessage("Reprocessing cancelled.\n");
                }
            }
            else
            {
                _transactionUserInteraction.ShowMessage($"\nNo unprocessed transactions found in date range '{TransactionFilterService.GetHumanReadableTransactionRange(transactionFilterString)}' (all transactions have Type assigned).\n");
            }

            // After reprocessing, show menu again
            _transactionUserInteraction.ShowMessage("\nWhat would you like to do?");
            _transactionUserInteraction.ShowMessage("1. Continue to transactions (default)");
            _transactionUserInteraction.ShowMessage("2. Edit profile");
            _transactionUserInteraction.ShowMessage("3. Category cleanup");
            _transactionUserInteraction.ShowMessage("4. Reprocess untyped transactions");
            _transactionUserInteraction.ShowMessage("5. Quit\n");

            userInput = _transactionUserInteraction.GetInput().Trim();

            // Input validation
            if (!new[] { "1", "2", "3", "4", "5", "" }.Contains(userInput))
            {
                _transactionUserInteraction.ShowMessage($"Invalid choice '{userInput}'. Using default (1).\n");
                userInput = "1";
            }

            if (userInput == "5")
            {
                _transactionUserInteraction.Exit();
            }
        }

        if (userInput == "5")
        {
            _transactionUserInteraction.Exit();
        }

        // Get new transactions from CSV repository
        if (transactionsDictionary != null)
        {
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
        }

        // fetch all transactions
        var rbcTransactions = await _rbcSqlRepository.GetAllAsync();
        var amexTransactions = await _amexSqlRepository.GetAllAsync();
        var pcTransactions = await _pcSqlRepository.GetAllAsync();

        var allTransactions = new List<Transaction>();
        allTransactions.AddRange(rbcTransactions);
        allTransactions.AddRange(amexTransactions);
        allTransactions.AddRange(pcTransactions);

        // Filter by date range BEFORE prompting user for vendor/category
        List<Transaction> filteredTransactions = TransactionFilterService.GetTransactionsInRange(allTransactions, transactionFilterString);

        // Filter by user BEFORE prompting (if applicable)
        if (profile.UserName != null)
        {
            filteredTransactions = TransactionFilterService.GetTransactionsForUser(filteredTransactions, profile.UserName);
        }

        // Now process only the filtered transactions - user only sees prompts for relevant date range
        List<Transaction> transactionsWithVendors = await _vendorsService.AddVendorsToTransactionsAsync(filteredTransactions);
        List<Transaction> transactionsWithCategories = await _categoriesService.AddCategoriesToTransactionsAsync(transactionsWithVendors, profile, _budgetService);

        // Persist Transaction.Type and other changes to database
        var rbcToUpdate = transactionsWithCategories.OfType<RBCTransaction>().ToList();
        var amexToUpdate = transactionsWithCategories.OfType<AmexTransaction>().ToList();
        var pcToUpdate = transactionsWithCategories.OfType<PCFinancialTransaction>().ToList();

        if (rbcToUpdate.Any())
            await _rbcSqlRepository.UpdateAsync(rbcToUpdate);
        if (amexToUpdate.Any())
            await _amexSqlRepository.UpdateAsync(amexToUpdate);
        if (pcToUpdate.Any())
            await _pcSqlRepository.UpdateAsync(pcToUpdate);

        // Update filteredTransactions to point to the categorized subset
        filteredTransactions = transactionsWithCategories;

        // Transfer Management Phase
        var transferCount = filteredTransactions.Count(t => t.Type == TransactionType.Transfer);
        if (transferCount > 0)
        {
            Console.Write($"\nFound {transferCount} transfer(s). Review transfers? (y/n): ");
            var reviewTransfers = Console.ReadLine()?.Trim().ToLower();

            if (reviewTransfers == "y")
            {
                try
                {
                    await _transferManagementService.ManageTransfersAsync(filteredTransactions, profile);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Transfer management cancelled.\n");
                }
            }
        }

        filteredTransactions = _categoriesService.OverrideCategories(filteredTransactions, "Restaurant", "Entertainment");

        string rangeType = TransactionFilterService.GetHumanReadableTransactionRange(transactionFilterString);
        string tableName = rangeType is not null ? $"{rangeType} Transactions" : "Transactions";

        // Get type-based transaction lists
        var budgetedExpenses = GetBudgetedExpenses(filteredTransactions);
        var trackedOnlyExpenses = GetTrackedOnlyExpenses(filteredTransactions);
        var transfers = GetTransfers(filteredTransactions);
        var income = GetIncome(filteredTransactions);
        var adjustments = GetAdjustments(filteredTransactions);

        // Output all transactions overview
        _transactionUserInteraction.OutputTransactions(filteredTransactions, tableName, null);

        // === SECTION 1: BUDGET CATEGORIES ===
        Console.WriteLine("\n═══════════════════════════════════════════════════════════");
        Console.WriteLine("                    BUDGET CATEGORIES");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        foreach (string category in categories)
        {
            var categorizedTransactions = budgetedExpenses
                .Where(transaction => transaction.Category == category)
                .ToList();

            if (profile.BudgetCategories.Any(c => c.Key.ToLower() == category.ToLower()) && categorizedTransactions.Any())
            {
                _transactionUserInteraction.OutputTransactions(categorizedTransactions, category, profile);
            }
        }

        // Output Budget Vs Actual for budgeted categories only
        _transactionUserInteraction.OutputBudgetVsActual(budgetedExpenses, profile);

        // === SECTION 2: FIXED OBLIGATIONS (Tracked Only) ===
        if (trackedOnlyExpenses.Any())
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("              FIXED OBLIGATIONS (Tracked Only)");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            var trackedCategories = trackedOnlyExpenses
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .Select(t => t.Category)
                .Distinct()
                .ToList();

            foreach (var category in trackedCategories)
            {
                var trackedTransactions = trackedOnlyExpenses
                    .Where(t => t.Category == category)
                    .ToList();

                _transactionUserInteraction.OutputTransactions(trackedTransactions, $"{category} (Tracked)", null);
            }

            var totalTrackedOnly = trackedOnlyExpenses.Sum(t => t.Amount);
            Console.WriteLine($"\nTotal Fixed Obligations: ${Math.Abs(totalTrackedOnly):N2}\n");
        }

        // === SECTION 3: ACCOUNT ACTIVITY (Transfers) ===
        if (transfers.Any())
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("                   ACCOUNT ACTIVITY");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            DisplayTransfers(transfers);
        }

        // === SECTION 4: INCOME ===
        if (income.Any())
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("                        INCOME");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            _transactionUserInteraction.OutputTransactions(income, "Income", null);
            Console.WriteLine($"\nTotal Income: ${Math.Abs(income.Sum(t => t.Amount)):N2}\n");
        }

        // === SECTION 5: ADJUSTMENTS ===
        if (adjustments.Any())
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("                      ADJUSTMENTS");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            _transactionUserInteraction.OutputTransactions(adjustments, "Adjustments (Fees, Rewards, etc.)", null);
            Console.WriteLine($"\nTotal Adjustments: ${adjustments.Sum(t => t.Amount):N2}\n");
        }

        // === UNCATEGORIZED TRANSACTIONS ===
        var uncategorizedExpenses = filteredTransactions
            .Where(t => t.Type == TransactionType.Expense && string.IsNullOrEmpty(t.Category))
            .ToList();

        if (uncategorizedExpenses.Any())
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("                   UNCATEGORIZED EXPENSES");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            _transactionUserInteraction.OutputTransactions(uncategorizedExpenses, "Uncategorized", null);
        }

        // === UNPROCESSED TRANSACTIONS ===
        var unprocessedTransactions = filteredTransactions
            .Where(t => t.Type == 0 || t.Type == default(TransactionType))
            .ToList();

        if (unprocessedTransactions.Any())
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("                ⚠ UNPROCESSED TRANSACTIONS");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            _transactionUserInteraction.OutputTransactions(unprocessedTransactions, "Unprocessed", null);
            Console.WriteLine($"\n⚠ {unprocessedTransactions.Count} transaction(s) need type classification.\n");
        }


        /* TODO:
        - [ ] create method to find specific trnansactions via name and amount to categorize as, rent, student loan, etc -- take one
        - [ ] create total expense vs diff
        - [ ] aim to get caluclates to become exact
        - [ ] in budget vs actual, determine unaccounted for transactions, ie. missing student loan etc

        */
        //_transactionCsvRepository.ExportTransactions(filteredTransactions, "./export-test.csv");
    }

    /// <summary>
    /// Get expenses that are budgeted (not tracked-only)
    /// </summary>
    private List<Transaction> GetBudgetedExpenses(List<Transaction> transactions)
    {
        return transactions
            .Where(t => t.Type == TransactionType.Expense && !IsCategoryTrackedOnly(t.Category))
            .ToList();
    }

    /// <summary>
    /// Get expenses that are tracked-only (not budgeted)
    /// </summary>
    private List<Transaction> GetTrackedOnlyExpenses(List<Transaction> transactions)
    {
        return transactions
            .Where(t => t.Type == TransactionType.Expense && IsCategoryTrackedOnly(t.Category))
            .ToList();
    }

    /// <summary>
    /// Get all transfers
    /// </summary>
    private List<Transaction> GetTransfers(List<Transaction> transactions)
    {
        return transactions
            .Where(t => t.Type == TransactionType.Transfer)
            .OrderBy(t => t.Date)
            .ToList();
    }

    /// <summary>
    /// Get all income transactions
    /// </summary>
    private List<Transaction> GetIncome(List<Transaction> transactions)
    {
        return transactions
            .Where(t => t.Type == TransactionType.Income)
            .ToList();
    }

    /// <summary>
    /// Get all adjustments (fees, rewards, etc.)
    /// </summary>
    private List<Transaction> GetAdjustments(List<Transaction> transactions)
    {
        return transactions
            .Where(t => t.Type == TransactionType.Adjustment)
            .ToList();
    }

    /// <summary>
    /// Check if a category is marked as tracked-only
    /// </summary>
    private bool IsCategoryTrackedOnly(string? categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
            return false;

        // Check in database via repository
        var categoriesRepo = _categoriesService as CategoriesService;
        if (categoriesRepo != null)
        {
            // Note: This would require adding a synchronous method to check IsTrackedOnly
            // For now, we'll assume categories with specific names are tracked-only
            // This can be enhanced later with proper database lookup
            return false; // Will be properly implemented when we have sync access to Category.IsTrackedOnly
        }

        return false;
    }

    /// <summary>
    /// Display transfers with special formatting showing matched pairs
    /// </summary>
    private void DisplayTransfers(List<Transaction> transfers)
    {
        var displayedIds = new HashSet<int>();

        foreach (var transfer in transfers)
        {
            if (displayedIds.Contains(transfer.Id))
                continue;

            // Check if this is part of a reconciled pair
            if (transfer.IsReconciledTransfer && !string.IsNullOrEmpty(transfer.LinkedTransactionId))
            {
                // Find the matching transfer
                var linkedTransfer = transfers.FirstOrDefault(t =>
                    t.LinkedTransactionId == transfer.LinkedTransactionId &&
                    t.Id != transfer.Id);

                if (linkedTransfer != null)
                {
                    // Display as a matched pair
                    var outTransfer = transfer.Amount < 0 ? transfer : linkedTransfer;
                    var inTransfer = transfer.Amount > 0 ? transfer : linkedTransfer;

                    var outAccount = GetFormattedAccountInfo(outTransfer);
                    var inAccount = GetFormattedAccountInfo(inTransfer);

                    Console.WriteLine($"{outTransfer.Date:MMM dd}  {outAccount,-20} {outTransfer.Description,-40} ${Math.Abs(outTransfer.Amount),10:N2} ↑ OUT ↔");
                    Console.WriteLine($"{inTransfer.Date:MMM dd}  {inAccount,-20} {inTransfer.Description,-40} ${Math.Abs(inTransfer.Amount),10:N2} ↓ IN   ✓ Reconciled\n");

                    displayedIds.Add(transfer.Id);
                    displayedIds.Add(linkedTransfer.Id);
                    continue;
                }
            }

            // Display as unmatched transfer
            var account = GetFormattedAccountInfo(transfer);
            var direction = transfer.Amount < 0 ? "↑ OUT" : "↓ IN ";
            Console.WriteLine($"{transfer.Date:MMM dd}  {account,-20} {transfer.Description,-40} ${Math.Abs(transfer.Amount),10:N2} {direction}  ⚠ Unmatched");

            displayedIds.Add(transfer.Id);
        }

        var totalTransfers = transfers.Sum(t => t.Amount);
        Console.WriteLine($"\nNet Transfer Activity: ${totalTransfers:N2}");
        Console.WriteLine($"(↑ = money out, ↓ = money in)\n");
    }

    /// <summary>
    /// Format account information with account number (last 4 digits) if available
    /// </summary>
    private string GetFormattedAccountInfo(Transaction transaction)
    {
        if (transaction is AmexTransaction amex && !string.IsNullOrEmpty(amex.AccountNumber))
        {
            // Get last 4 digits of account number
            var last4 = amex.AccountNumber.Length > 4
                ? amex.AccountNumber.Substring(amex.AccountNumber.Length - 4)
                : amex.AccountNumber;
            return $"{transaction.AccountType} (*{last4})";
        }

        return transaction.AccountType;
    }
}