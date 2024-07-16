using PersonalFinances.Repositories;
using PersonalFinances.Models;

namespace PersonalFinances.App;
class PersonalFinancesApp
{
    private readonly ITransactionsRepository _transactionRepository;
    private readonly ITransactionsUserInteraction _transactionUserInteraction;
    private readonly IVendorsService _vendorsService;
    private readonly ICategoriesService _categoriesService;
    private readonly IBudgetService _budgetService;

    public PersonalFinancesApp(
        ITransactionsRepository transactionRepository, 
        ITransactionsUserInteraction transactionUserInteraction,
        IVendorsService vendorsService,
        ICategoriesService categoriesService,
        IBudgetService budgetService
        )
    {
        _transactionRepository = transactionRepository;
        _transactionUserInteraction = transactionUserInteraction;
        _vendorsService = vendorsService; 
        _categoriesService = categoriesService;
        _budgetService = budgetService;
    }

    public void Run(string transactionsFilePath, TransactionFilterService.TransactionRange? transactionFilterString)
    {
        Console.WriteLine("Finances App Initialized\n");
        List<string> categories = _categoriesService.GetAllCategories();

        BudgetProfile? profile = _budgetService.GetActiveProfile();
        if (profile is null)
        {
            _transactionUserInteraction.ShowMessage("No budget profiles found. Creating first profile...");
            profile = _budgetService.CreateNewProfile();
        }

        double budgetTotal = _budgetService.GetBudgetTotal(profile);

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

        List<Transaction> rawTransactions = _transactionRepository.GetTransactions(transactionsFilePath);
        List<Transaction> transactionsWithVendors = _vendorsService.AddVendorsToTransactions(rawTransactions);
        List<Transaction> transactionsWithCategories = _categoriesService.AddCategoriesToTransactions(transactionsWithVendors);
        List<Transaction> filteredTransactions = TransactionFilterService.GetTransactionsInRange(transactionsWithCategories, transactionFilterString);
        List<Transaction> spendingTransactions = TransactionFilterService.GetSpendingTransactions(filteredTransactions);
        
        string rangeType = TransactionFilterService.GetHumanReadableTransactionRange(transactionFilterString);
        string tableName = rangeType is not null ? $"{rangeType}'s Transactions" : "Transactions";
        _transactionUserInteraction.OutputTransactions(spendingTransactions, tableName, null);

        foreach (string category in categories)
        {
            List<Transaction> categorizedTransactions = filteredTransactions.Where(transaction => transaction.Category == category).ToList();
            _transactionUserInteraction.OutputTransactions(categorizedTransactions, category, profile);
        }

        _transactionRepository.ExportTransactions(filteredTransactions, "");
    }
}