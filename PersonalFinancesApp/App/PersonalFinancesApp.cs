using PersonalFinances.Repositories;
using PersonalFinances.Models;
using System.ComponentModel;
using System.Runtime;

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

    public void Run(string transactionsFilePath)
    {
        Console.WriteLine("Finances App Initialized\n");
        List<string> categories = _categoriesService.GetAllCategories();

        BudgetProfile? profile = _budgetService.GetActiveProfile();
        if (profile is null)
        {
            // TODO: create budget categories ahead of time
            _transactionUserInteraction.ShowMessage("No budget profiles found. Creating first profile...");
            profile = _budgetService.CreateNewProfile();
        }

        // TODO: output profile. Ask user if it is okay or would like to edit
        _transactionUserInteraction.ShowMessage($"Budget profile set to:\n");
        _transactionUserInteraction.ShowMessage(profile.ToString());

        _transactionUserInteraction.ShowMessage("\nPress 'q' to quit, or enter to continue...");
        var input = _transactionUserInteraction.GetInput();
        if (input == "q")
        {
            _transactionUserInteraction.Exit();
        }

        // pass profile int

        List<Transaction> rawTransactions = _transactionRepository.GetTransactions(transactionsFilePath);
        List<Transaction> transactionsWithVendors = _vendorsService.AddVendorsToTransactions(rawTransactions);
        List<Transaction> transactionsWithCategories = _categoriesService.AddCategoriesToTransactions(transactionsWithVendors);

        // TODO: create enums for date ranges. Create date filter class -> pass in transactions
        // DateTime lastMonth = LastDayOfLastMonth();
        // List<Transaction> monthlyTransactions = transactionsWithVendors.Where(transaction => transaction.Date > lastMonth).ToList();

        // Output all transactions
        _transactionUserInteraction.OutputTransactions(transactionsWithCategories, "All Transactions", null);

        // Output by category
        foreach (string category in categories)
        {
            List<Transaction> categorizedTransactions = transactionsWithCategories.Where(transaction => transaction.Category == category).ToList();
            _transactionUserInteraction.OutputTransactions(categorizedTransactions, category, profile);
        }

        // TODO: write report
        /*
            show each category, plus minus and percentages used. How much under or over budget
        */
    }

    public void Run()
    {
        _transactionUserInteraction.ShowMessage("Please enter a path to your transactions:");
        string transactionsPath = _transactionUserInteraction.GetInput();
        Console.WriteLine($"Transaction path is {transactionsPath}");

        _transactionUserInteraction.ShowMessage("Please enter a path to your list of saved vendors:");
        string vendorsPath = _transactionUserInteraction.GetInput();
        Console.WriteLine($"Vendors path is {vendorsPath}");

        // TODO: complete method call this(transactionPath?)
    }

    private DateTime LastDayOfLastMonth() 
    {
        DateTime firstDayofThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0);
        return firstDayofThisMonth.AddSeconds(-1);
    }
}