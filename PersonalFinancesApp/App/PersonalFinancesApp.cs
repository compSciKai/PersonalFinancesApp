using PersonalFinances.Repositories;
using PersonalFinances.Models;
using System.ComponentModel;

namespace PersonalFinances.App;
class PersonalFinancesApp
{
    private readonly ITransactionsRepository _transactionRepository;
    private readonly ITransactionsUserInteraction _transactionUserInteraction;
    private readonly IVendorsService _vendorsService;
    private readonly ICategoriesService _categoriesService;

    public PersonalFinancesApp(
        ITransactionsRepository transactionRepository, 
        ITransactionsUserInteraction transactionUserInteraction,
        IVendorsService vendorsService,
        ICategoriesService categoriesService
        )
    {
        _transactionRepository = transactionRepository;
        _transactionUserInteraction = transactionUserInteraction;
        _vendorsService = vendorsService; 
        _categoriesService = categoriesService;
    }

    public void Run(string transactionsFilePath)
    {
        Console.WriteLine("Finances App Initialized");

        List<Transaction> rawTransactions = _transactionRepository.GetTransactions(transactionsFilePath);
        List<Transaction> transactionsWithVendors = _vendorsService.AddVendorsToTransactions(rawTransactions);
        List<Transaction> transactionsWithCategories = _categoriesService.AddCategoriesToTransactions(transactionsWithVendors);

        // DateTime lastMonth = LastDayOfLastMonth();
        // List<Transaction> monthlyTransactions = transactionsWithVendors.Where(transaction => transaction.Date > lastMonth).ToList();

        // Output all transactions
        _transactionUserInteraction.OutputTransactions(transactionsWithCategories, "All Transactions");

        // Output by category
        List<string> categories = _categoriesService.GetAllCategories();

        foreach (string category in categories)
        {
            List<Transaction> categorizedTransactions = transactionsWithCategories.Where(transaction => transaction.Category == category).ToList();
            _transactionUserInteraction.OutputTransactions(categorizedTransactions, category);
        }
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