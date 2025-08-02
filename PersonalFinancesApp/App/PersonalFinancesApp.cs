using PersonalFinances.Repositories;
using PersonalFinances.Models;

namespace PersonalFinances.App;
class PersonalFinancesApp
{
    private readonly ITransactionsRepository _transactionCsvRepository;
    private readonly ITransactionsRepository _transactionSqlRepository;
    private readonly ITransactionsUserInteraction _transactionUserInteraction;
    private readonly IVendorsService _vendorsService;
    private readonly ICategoriesService _categoriesService;
    private readonly IBudgetService _budgetService;

    public PersonalFinancesApp(
        ITransactionsRepository transactionCsvRepository,
        ITransactionsRepository transactionSqlRepository, 
        ITransactionsUserInteraction transactionUserInteraction,
        IVendorsService vendorsService,
        ICategoriesService categoriesService,
        IBudgetService budgetService
        )
    {
        _transactionCsvRepository = transactionCsvRepository;
        _transactionSqlRepository = transactionSqlRepository;
        _transactionUserInteraction = transactionUserInteraction;
        _vendorsService = vendorsService; 
        _categoriesService = categoriesService;
        _budgetService = budgetService;
    }

    public void Run(Dictionary<string, Type> transactionsDictionary, TransactionFilterService.TransactionRange? transactionFilterString, string? currentProfile)
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
        List<Transaction> newTransactions = new List<Transaction>();

        foreach (var transactionEntry in transactionsDictionary)
        {
            if (transactionEntry.Value == typeof(RBCTransaction))
            {
                var transactions = _transactionCsvRepository.GetTransactions<RBCTransaction>(transactionEntry.Key);
                newTransactions.AddRange(transactions);
            }
            else if (transactionEntry.Value == typeof(AmexTransaction))
            {
                var transactions = _transactionCsvRepository.GetTransactions<AmexTransaction>(transactionEntry.Key);
                newTransactions.AddRange(transactions);
            }
            else if (transactionEntry.Value == typeof(PCFinancialTransaction))
            {
                var transactions = _transactionCsvRepository.GetTransactions<PCFinancialTransaction>(transactionEntry.Key);
                newTransactions.AddRange(transactions);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported transaction type: {transactionEntry.Value}");
            }
        }

        // save new transactions
        _transactionSqlRepository.SaveTransactionsWithHashAsync(newTransactions);

        // fetch all transactions
        var allTransactions = _transactionSqlRepository.GetTransactions<Transaction>();

        // process transactions if missing fields

        // output information 







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