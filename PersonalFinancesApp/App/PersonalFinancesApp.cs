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
    private readonly ICsvImportOrchestrator _csvImportOrchestrator;
    private readonly ICsvFileArchiveService _csvFileArchiveService;
    private readonly ICsvFetchService _csvFetchService;
    private Dictionary<string, bool> _categoryTrackedOnlyCache = new();

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
        ITransactionReprocessingService reprocessingService,
        ICsvImportOrchestrator csvImportOrchestrator,
        ICsvFileArchiveService csvFileArchiveService,
        ICsvFetchService csvFetchService
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
        _csvImportOrchestrator = csvImportOrchestrator;
        _csvFileArchiveService = csvFileArchiveService;
        _csvFetchService = csvFetchService;
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

    private async Task LoadCategoriesAsync()
    {
        try
        {
            // Try to get the database repository
            var categoriesService = _categoriesService as CategoriesService;
            if (categoriesService != null)
            {
                // Access the internal repository (assuming it's DatabaseCategoriesRepository)
                var dbRepoField = categoriesService.GetType().GetField("_categoriesRepository",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (dbRepoField != null)
                {
                    var dbRepo = dbRepoField.GetValue(categoriesService) as DatabaseCategoriesRepository;
                    if (dbRepo != null)
                    {
                        var categories = await dbRepo.GetAllCategoriesAsync();
                        _categoryTrackedOnlyCache = categories.ToDictionary(
                            c => c.CategoryName,
                            c => c.IsTrackedOnly,
                            StringComparer.OrdinalIgnoreCase
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _transactionUserInteraction.ShowMessage($"Warning: Could not load category cache: {ex.Message}");
        }
    }

    public async Task RunAsync(Dictionary<string, Type> transactionsDictionary, TransactionFilterService.TransactionRange? transactionFilterString, CsvImportSettings? csvImportSettings)
    {
        // load data from sources
        Console.WriteLine("CashFlow App Initialized\n");

        // Load category cache for tracked-only detection
        await LoadCategoriesAsync();

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
        _transactionUserInteraction.ShowMessage("0. Fetch new transactions from banks");
        _transactionUserInteraction.ShowMessage("1. Continue to transactions (default)");
        _transactionUserInteraction.ShowMessage("2. Edit profile");
        _transactionUserInteraction.ShowMessage("3. Category cleanup");
        _transactionUserInteraction.ShowMessage("4. Reprocess untyped transactions");
        _transactionUserInteraction.ShowMessage("5. Quit\n");

        string userInput = _transactionUserInteraction.GetInput().Trim();

        // Input validation
        if (!new[] { "0", "1", "2", "3", "4", "5", "" }.Contains(userInput))
        {
            _transactionUserInteraction.ShowMessage($"Invalid choice '{userInput}'. Using default (1).\n");
            userInput = "1";
        }

        // Handle option 0: Fetch transactions from banks
        if (userInput == "0")
        {
            // Get list of enabled vendors
            var enabledVendors = new List<(string Name, string DisplayName)>();

            if (csvImportSettings?.RBC?.AutomatedFetch?.Enabled == true)
                enabledVendors.Add(("RBC", "RBC"));
            if (csvImportSettings?.Amex?.AutomatedFetch?.Enabled == true)
                enabledVendors.Add(("AMEX", "American Express"));
            if (csvImportSettings?.PCFinancial?.AutomatedFetch?.Enabled == true)
                enabledVendors.Add(("PCFINANCIAL", "PC Financial"));

            if (!enabledVendors.Any())
            {
                _transactionUserInteraction.ShowMessage("❌ No vendors have automated fetching enabled in appsettings.json\n");
            }
            else if (enabledVendors.Count == 1)
            {
                // Only one vendor enabled, fetch directly
                var vendor = enabledVendors[0];
                _transactionUserInteraction.ShowMessage($"\n🌐 Fetching new transactions from {vendor.DisplayName}...");
                var fetchResult = await _csvFetchService.FetchTransactionsForVendorAsync(vendor.Name);

                if (fetchResult.Success)
                {
                    _transactionUserInteraction.ShowMessage($"✅ Success! Downloaded {fetchResult.FilesDownloaded} file(s)");
                    if (!string.IsNullOrEmpty(fetchResult.DownloadedFilePath))
                    {
                        _transactionUserInteraction.ShowMessage($"   File: {System.IO.Path.GetFileName(fetchResult.DownloadedFilePath)}\n");
                    }
                }
                else
                {
                    _transactionUserInteraction.ShowMessage($"❌ Error: {fetchResult.ErrorMessage}\n");
                }
            }
            else
            {
                // Multiple vendors enabled, show selection menu
                _transactionUserInteraction.ShowMessage("\n📋 Select vendor to fetch from:");
                _transactionUserInteraction.ShowMessage("0. All enabled vendors");
                for (int i = 0; i < enabledVendors.Count; i++)
                {
                    _transactionUserInteraction.ShowMessage($"{i + 1}. {enabledVendors[i].DisplayName}");
                }
                _transactionUserInteraction.ShowMessage("");

                var vendorChoice = _transactionUserInteraction.GetInput().Trim();

                List<(string Name, string DisplayName)> vendorsToFetch;
                if (vendorChoice == "0")
                {
                    vendorsToFetch = enabledVendors;
                }
                else if (int.TryParse(vendorChoice, out int choice) && choice >= 1 && choice <= enabledVendors.Count)
                {
                    vendorsToFetch = new List<(string, string)> { enabledVendors[choice - 1] };
                }
                else
                {
                    _transactionUserInteraction.ShowMessage($"Invalid choice '{vendorChoice}'. Skipping fetch.\n");
                    vendorsToFetch = new List<(string, string)>();
                }

                // Fetch from selected vendor(s)
                foreach (var vendor in vendorsToFetch)
                {
                    _transactionUserInteraction.ShowMessage($"\n🌐 Fetching new transactions from {vendor.DisplayName}...");
                    var fetchResult = await _csvFetchService.FetchTransactionsForVendorAsync(vendor.Name);

                    if (fetchResult.Success)
                    {
                        _transactionUserInteraction.ShowMessage($"✅ Success! Downloaded {fetchResult.FilesDownloaded} file(s)");
                        if (!string.IsNullOrEmpty(fetchResult.DownloadedFilePath))
                        {
                            _transactionUserInteraction.ShowMessage($"   File: {System.IO.Path.GetFileName(fetchResult.DownloadedFilePath)}\n");
                        }
                    }
                    else
                    {
                        _transactionUserInteraction.ShowMessage($"❌ Error: {fetchResult.ErrorMessage}\n");
                    }
                }
            }

            // Return to menu
            _transactionUserInteraction.ShowMessage("\nWhat would you like to do?");
            _transactionUserInteraction.ShowMessage("0. Fetch new transactions from banks");
            _transactionUserInteraction.ShowMessage("1. Continue to transactions (default)");
            _transactionUserInteraction.ShowMessage("2. Edit profile");
            _transactionUserInteraction.ShowMessage("3. Category cleanup");
            _transactionUserInteraction.ShowMessage("4. Reprocess untyped transactions");
            _transactionUserInteraction.ShowMessage("5. Quit\n");

            userInput = _transactionUserInteraction.GetInput().Trim();

            // Input validation
            if (!new[] { "0", "1", "2", "3", "4", "5", "" }.Contains(userInput))
            {
                _transactionUserInteraction.ShowMessage($"Invalid choice '{userInput}'. Using default (1).\n");
                userInput = "1";
            }
        }

        // Prompt for transaction range if user chose to continue to transactions
        if (userInput == "1" || userInput == "")
        {
            transactionFilterString = _transactionUserInteraction.PromptForTransactionRange();

            string selectedRangeDisplay = TransactionFilterService.GetHumanReadableTransactionRange(transactionFilterString);
            _transactionUserInteraction.ShowMessage($"\nTransaction range set to: {selectedRangeDisplay}\n");
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
                    _transactionUserInteraction.ShowMessage("0. Fetch new transactions from banks");
                    _transactionUserInteraction.ShowMessage("1. Continue to transactions (default)");
                    _transactionUserInteraction.ShowMessage("2. Edit profile");
                    _transactionUserInteraction.ShowMessage("3. Category cleanup");
                    _transactionUserInteraction.ShowMessage("4. Reprocess untyped transactions");
                    _transactionUserInteraction.ShowMessage("5. Quit\n");

                    userInput = _transactionUserInteraction.GetInput().Trim();

                    // Input validation
                    if (!new[] { "0", "1", "2", "3", "4", "5", "" }.Contains(userInput))
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
            _transactionUserInteraction.ShowMessage("0. Fetch new transactions from banks");
            _transactionUserInteraction.ShowMessage("1. Continue to transactions (default)");
            _transactionUserInteraction.ShowMessage("2. Edit profile");
            _transactionUserInteraction.ShowMessage("3. Category cleanup");
            _transactionUserInteraction.ShowMessage("4. Reprocess untyped transactions");
            _transactionUserInteraction.ShowMessage("5. Quit\n");

            userInput = _transactionUserInteraction.GetInput().Trim();

            // Input validation
            if (!new[] { "0", "1", "2", "3", "4", "5", "" }.Contains(userInput))
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
            _transactionUserInteraction.ShowMessage("0. Fetch new transactions from banks");
            _transactionUserInteraction.ShowMessage("1. Continue to transactions (default)");
            _transactionUserInteraction.ShowMessage("2. Edit profile");
            _transactionUserInteraction.ShowMessage("3. Category cleanup");
            _transactionUserInteraction.ShowMessage("4. Reprocess untyped transactions");
            _transactionUserInteraction.ShowMessage("5. Quit\n");

            userInput = _transactionUserInteraction.GetInput().Trim();

            // Input validation
            if (!new[] { "0", "1", "2", "3", "4", "5", "" }.Contains(userInput))
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

        // Get transactions dictionary: prioritize hardcoded dict, then scan folders
        Console.WriteLine("\n🔄 Determining transaction sources...");
        var transactionsToProcess = await _csvImportOrchestrator.GetTransactionsToProcessAsync(
            transactionsDictionary,
            csvImportSettings,
            _transactionUserInteraction);

        if (transactionsToProcess == null || transactionsToProcess.Count == 0)
        {
            Console.WriteLine("No transactions to process.\n");
        }
        else
        {
            Console.WriteLine("🔄 Loading transactions from CSV files...");

            foreach (var transactionEntry in transactionsToProcess)
            {
                if (string.IsNullOrEmpty(transactionEntry.Key))
                {
                    continue; // Skip empty keys
                }

                try
                {
                    List<Transaction>? loadedTransactions = null;

                    if (transactionEntry.Value == typeof(RBCTransaction))
                    {
                        var loadedRbcTransactions = await _rbcCsvRepository.LoadFromFileAsync(transactionEntry.Key);
                        await _rbcSqlRepository.SaveAsync(loadedRbcTransactions);
                        loadedTransactions = loadedRbcTransactions.Cast<Transaction>().ToList();
                        Console.WriteLine($"  ✓ Loaded {loadedTransactions.Count} RBC transactions from {Path.GetFileName(transactionEntry.Key)}");
                    }
                    else if (transactionEntry.Value == typeof(AmexTransaction))
                    {
                        var loadedAmexTransactions = await _amexCsvRepository.LoadFromFileAsync(transactionEntry.Key);
                        await _amexSqlRepository.SaveAsync(loadedAmexTransactions);
                        loadedTransactions = loadedAmexTransactions.Cast<Transaction>().ToList();
                        Console.WriteLine($"  ✓ Loaded {loadedTransactions.Count} Amex transactions from {Path.GetFileName(transactionEntry.Key)}");
                    }
                    else if (transactionEntry.Value == typeof(PCFinancialTransaction))
                    {
                        var loadedPcTransactions = await _pcCsvRepository.LoadFromFileAsync(transactionEntry.Key);
                        await _pcSqlRepository.SaveAsync(loadedPcTransactions);
                        loadedTransactions = loadedPcTransactions.Cast<Transaction>().ToList();
                        Console.WriteLine($"  ✓ Loaded {loadedTransactions.Count} PC Financial transactions from {Path.GetFileName(transactionEntry.Key)}");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unsupported transaction type: {transactionEntry.Value}");
                    }

                    // Archive file if successful and settings available
                    if (loadedTransactions?.Count > 0 && csvImportSettings != null)
                    {
                        string? processedFolder = GetProcessedFolderForType(transactionEntry.Value, csvImportSettings);
                        if (!string.IsNullOrEmpty(processedFolder))
                        {
                            bool archived = await _csvFileArchiveService.ArchiveFileAsync(
                                transactionEntry.Key,
                                processedFolder,
                                loadedTransactions);

                            if (archived)
                            {
                                Console.WriteLine($"  ✓ Archived: {Path.GetFileName(transactionEntry.Key)}");
                            }
                            else
                            {
                                Console.WriteLine($"  ⚠ Failed to archive: {Path.GetFileName(transactionEntry.Key)} (file remains in input folder)");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Error processing {Path.GetFileName(transactionEntry.Key)}: {ex.Message}");
                    Console.WriteLine($"    File remains in input folder for inspection.\n");
                    // Continue to next file instead of throwing
                }
            }

            Console.WriteLine();
        }

        // fetch all transactions
        Console.WriteLine("🔄 Fetching transactions from database...");
        var rbcTransactions = await _rbcSqlRepository.GetAllAsync();
        var amexTransactions = await _amexSqlRepository.GetAllAsync();
        var pcTransactions = await _pcSqlRepository.GetAllAsync();
        Console.WriteLine($"✓ Loaded {rbcTransactions.Count + amexTransactions.Count + pcTransactions.Count} transactions from database\n");

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
        Console.WriteLine($"🔄 Processing {filteredTransactions.Count} transactions in selected range...\n");
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

        // filteredTransactions = _categoriesService.OverrideCategories(filteredTransactions, "Restaurant", "Entertainment");

        string rangeType = TransactionFilterService.GetHumanReadableTransactionRange(transactionFilterString);
        string tableName = rangeType is not null ? $"{rangeType} Transactions" : "Transactions";

        // Get type-based transaction lists
        var budgetedExpenses = GetBudgetedExpenses(filteredTransactions);
        var trackedOnlyExpenses = GetTrackedOnlyExpenses(filteredTransactions);
        var transfers = GetTransfers(filteredTransactions);
        var income = GetIncome(filteredTransactions);
        var adjustments = GetAdjustments(filteredTransactions);
        var unbudgetedExpenses = GetUnbudgetedExpenses(filteredTransactions, profile);

        // Output all transactions overview
        _transactionUserInteraction.OutputTransactions(filteredTransactions, tableName, null);

        // === SECTION 1: BUDGET CATEGORIES ===
        Console.WriteLine("\n═══════════════════════════════════════════════════════════");
        Console.WriteLine("                    BUDGET CATEGORIES");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        // Loop through budget profile categories to ensure all budgeted categories get tables
        foreach (string category in profile.BudgetCategories.Keys)
        {
            var categorizedTransactions = budgetedExpenses
                .Where(transaction => string.Equals(transaction.Category, category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(transaction => transaction.Date)
                .ToList();

            if (categorizedTransactions.Any())
            {
                _transactionUserInteraction.OutputTransactions(categorizedTransactions, category, profile);
            }
        }

        // Output Budget Vs Actual for budgeted categories only

        _transactionUserInteraction.OutputBudgetVsActual(budgetedExpenses, profile);

        // === SECTION 1.5: UNBUDGETED CATEGORIES ===
        DisplayUnbudgetedCategories(unbudgetedExpenses);

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
                    .Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => t.Date)
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

        // === SECTION 5: UNCATEGORIZED ADJUSTMENTS ===
        if (adjustments.Any())
        {
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine("              UNCATEGORIZED ADJUSTMENTS");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            _transactionUserInteraction.OutputTransactions(adjustments, "Uncategorized Adjustments", null);
            Console.WriteLine($"\nTotal Uncategorized Adjustments: ${adjustments.Sum(t => t.Amount):N2}\n");
        }

        // === UNCATEGORIZED TRANSACTIONS ===
        var uncategorizedExpenses = filteredTransactions
            .Where(t => t.Type == TransactionType.Expense && string.IsNullOrEmpty(t.Category))
            .OrderBy(t => t.Date)
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
            .OrderBy(t => t.Date)
            .ToList();

        // === TRANSACTION RECONCILIATION VALIDATION ===
        // Ensure every transaction is accounted for in exactly one group
        var totalInGroups = budgetedExpenses.Count + trackedOnlyExpenses.Count +
                           transfers.Count + income.Count + adjustments.Count +
                           uncategorizedExpenses.Count + unprocessedTransactions.Count;

        if (totalInGroups != filteredTransactions.Count)
        {
            Console.WriteLine($"\n⚠️  WARNING: Transaction count mismatch!");
            Console.WriteLine($"   Total transactions: {filteredTransactions.Count}");
            Console.WriteLine($"   Accounted for: {totalInGroups}");
            Console.WriteLine($"   Difference: {filteredTransactions.Count - totalInGroups}");
            Console.WriteLine("   Some transactions may be missing or double-counted!\n");
        }

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
    /// Get expenses that are budgeted (not tracked-only), including categorized adjustments
    /// </summary>
    private List<Transaction> GetBudgetedExpenses(List<Transaction> transactions)
    {
        return transactions
            .Where(t =>
                (t.Type == TransactionType.Expense || t.Type == TransactionType.Adjustment) &&
                !string.IsNullOrEmpty(t.Category) &&  // Exclude uncategorized (handled separately)
                !IsCategoryTrackedOnly(t.Category))   // Exclude tracked-only expenses
            .OrderBy(t => t.Date)
            .ToList();
    }

    /// <summary>
    /// Get expenses that are tracked-only (not budgeted), including categorized adjustments
    /// </summary>
    private List<Transaction> GetTrackedOnlyExpenses(List<Transaction> transactions)
    {
        return transactions
            .Where(t => (t.Type == TransactionType.Expense || t.Type == TransactionType.Adjustment) &&
                        IsCategoryTrackedOnly(t.Category))
            .OrderBy(t => t.Date)
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
            .OrderBy(t => t.Date)
            .ToList();
    }

    /// <summary>
    /// Get uncategorized adjustments only (categorized adjustments are grouped with their categories)
    /// </summary>
    private List<Transaction> GetAdjustments(List<Transaction> transactions)
    {
        return transactions
            .Where(t => t.Type == TransactionType.Adjustment && string.IsNullOrEmpty(t.Category))
            .OrderBy(t => t.Date)
            .ToList();
    }

    /// <summary>
    /// Get expense transactions with categories NOT in the budget profile
    /// </summary>
    private List<Transaction> GetUnbudgetedExpenses(List<Transaction> transactions, BudgetProfile? profile)
    {
        // If no budget profile, consider all categorized expenses as budgeted
        // (fallback to existing behavior - don't show as unbudgeted)
        if (profile == null || !profile.BudgetCategories.Any())
            return new List<Transaction>();

        // Create case-insensitive set of budget category names
        var budgetCategoryNames = new HashSet<string>(
            profile.BudgetCategories.Keys,
            StringComparer.OrdinalIgnoreCase);

        return transactions
            .Where(t =>
                t.Type == TransactionType.Expense &&
                !string.IsNullOrEmpty(t.Category) &&
                !budgetCategoryNames.Contains(t.Category) &&
                !IsCategoryTrackedOnly(t.Category))
            .OrderBy(t => t.Date)
            .ToList();
    }

    /// <summary>
    /// Check if a category is marked as tracked-only
    /// </summary>
    private bool IsCategoryTrackedOnly(string? categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
            return false;

        // Use the cache loaded during initialization
        return _categoryTrackedOnlyCache.TryGetValue(categoryName, out bool isTrackedOnly)
            ? isTrackedOnly
            : false;
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
                    // Display as a matched pair with correct directions for each transaction
                    var direction1 = GetTransferDirection(transfer);
                    var direction2 = GetTransferDirection(linkedTransfer);

                    var account1 = GetFormattedAccountInfo(transfer);
                    var account2 = GetFormattedAccountInfo(linkedTransfer);

                    Console.WriteLine($"{transfer.Date:MMM dd}  {account1,-20} {transfer.Description,-40} ${Math.Abs(transfer.Amount),10:N2} {direction1} ↔");
                    Console.WriteLine($"{linkedTransfer.Date:MMM dd}  {account2,-20} {linkedTransfer.Description,-40} ${Math.Abs(linkedTransfer.Amount),10:N2} {direction2}  ✓ Reconciled\n");

                    displayedIds.Add(transfer.Id);
                    displayedIds.Add(linkedTransfer.Id);
                    continue;
                }
            }

            // Display as unmatched transfer
            var account = GetFormattedAccountInfo(transfer);
            var direction = GetTransferDirection(transfer);
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

    /// <summary>
    /// Get transfer direction (IN/OUT) respecting bank-specific amount conventions
    /// </summary>
    private string GetTransferDirection(Transaction transaction)
    {
        if (transaction.isNegativeAmounts)
        {
            // RBC, PC Financial: negative amounts = money out, positive = money in
            return transaction.Amount < 0 ? "↑ OUT" : "↓ IN ";
        }
        else
        {
            // Amex: positive amounts = money out, negative = money in
            return transaction.Amount > 0 ? "↑ OUT" : "↓ IN ";
        }
    }

    /// <summary>
    /// Display unbudgeted categories section
    /// </summary>
    private void DisplayUnbudgetedCategories(List<Transaction> unbudgetedExpenses)
    {
        if (!unbudgetedExpenses.Any())
            return;

        // Group by category
        var categoryGroups = unbudgetedExpenses
            .GroupBy(t => t.Category)
            .OrderBy(g => g.Key);

        // Calculate summary stats
        int categoryCount = categoryGroups.Count();
        int transactionCount = unbudgetedExpenses.Count;
        decimal totalAmount = unbudgetedExpenses.Sum(t => Math.Abs(t.Amount));

        // Display summary
        Console.WriteLine($"\nFound {categoryCount} unbudgeted {(categoryCount == 1 ? "category" : "categories")} " +
                         $"({transactionCount} {(transactionCount == 1 ? "transaction" : "transactions")}, " +
                         $"${totalAmount:N2} total)\n");

        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("                 UNBUDGETED CATEGORIES");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        // Display each category
        foreach (var group in categoryGroups)
        {
            string categoryName = group.Key ?? "Unknown";
            var categoryTransactions = group.OrderBy(t => t.Date).ToList();

            // Display transactions for this category (table header already includes category name)
            _transactionUserInteraction.OutputTransactions(categoryTransactions, categoryName, null);

            // Calculate subtotal for this category
            decimal subtotal = categoryTransactions.Sum(t => Math.Abs(t.Amount));
            Console.WriteLine($"  Subtotal: ${subtotal:N2}\n");
        }

        // Display total
        Console.WriteLine($"Total Unbudgeted Spending: ${totalAmount:N2}\n");
    }

    private string? GetProcessedFolderForType(Type transactionType, CsvImportSettings settings)
    {
        if (transactionType == typeof(RBCTransaction))
            return settings.RBC?.ProcessedFolder;
        else if (transactionType == typeof(AmexTransaction))
            return settings.Amex?.ProcessedFolder;
        else if (transactionType == typeof(PCFinancialTransaction))
            return settings.PCFinancial?.ProcessedFolder;
        return null;
    }
}