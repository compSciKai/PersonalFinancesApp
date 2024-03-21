using PersonalFinances.Repositories;
using PersonalFinances.Models;

namespace PersonalFinances.App;
class PersonalFinancesApp
{
    private readonly ITransactionsRepository _transactionRepository;
    private readonly ITransactionsUserInteraction _transactionUserInteraction;
    private readonly IVendorsService _vendorsService;

    public PersonalFinancesApp(
        ITransactionsRepository transactionRepository, 
        ITransactionsUserInteraction transactionUserInteraction,
        IVendorsService vendorsService
        )
    {
        _transactionRepository = transactionRepository;
        _transactionUserInteraction = transactionUserInteraction;
        _vendorsService = vendorsService; 
    }

    public void Run(string transactionsFilePath)
    {
        Console.WriteLine("Finances App Initialized");


        List<Transaction> rawTransactions = _transactionRepository.GetTransactions(transactionsFilePath);
        List<Transaction> transactionsWithVendors = _vendorsService.AddVendorsToTransactions(rawTransactions);

        DateTime lastMonth = LastDayOfLastMonth();
        List<Transaction> monthlyTransactions = transactionsWithVendors.Where(transaction => transaction.Date > lastMonth).ToList();

        _transactionUserInteraction.OutputTransactions(monthlyTransactions);

        // TODO: categorize

        // // Categorize Transactions
        // FinancialApp.OutputCategorized(transactions);
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