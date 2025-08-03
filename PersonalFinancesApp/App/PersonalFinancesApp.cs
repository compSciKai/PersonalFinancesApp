using PersonalFinances.Repositories;
using PersonalFinances.Models;

namespace PersonalFinances.App;
class PersonalFinancesApp
{
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

    public async Task RunAsync(Dictionary<string, Type> transactionsDictionary, TransactionFilterService.TransactionRange? transactionFilterString, string? currentProfile)
    {
        // load data from sources
        Console.WriteLine("Finances App Initialized\n");
        List<string> categories = _categoriesService.GetAllCategories();

        BudgetProfile? profile = _budgetService.GetProfile(currentProfile);
        if (profile is null) 
        {
            profile = _budgetService.GetActiveProfile();
            if (profile is null)
            {
                _transactionUserInteraction.ShowMessage("No budget profiles found. Creating first profile...");
                profile = _budgetService.CreateNewProfile();
            }
        }

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