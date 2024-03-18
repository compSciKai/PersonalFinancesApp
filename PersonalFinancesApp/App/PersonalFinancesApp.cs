using PersonalFinances.Repositories;
namespace PersonalFinances.App;
class PersonalFinancesApp
{
    private readonly ITransactionsRepository _transactionRepository;
    private readonly ITransactionsUserInteraction _transactionUserInteraction;

    public PersonalFinancesApp(ITransactionsRepository transactionRepository, ITransactionsUserInteraction transactionUserInteraction)
    {
        _transactionRepository = transactionRepository;
        _transactionUserInteraction = transactionUserInteraction;
    }

    public void Run(string newTransactionsFilePath)
    {
        Console.WriteLine("Finances App Initialized");

        string transactionsPath;
        // Get Transactions
        transactionsPath = newTransactionsFilePath;

        Console.WriteLine($"Transaction path is {transactionsPath}");

        _transactionRepository.GetTransactions(transactionsPath);
    }

    public void Run()
    {
        _transactionUserInteraction.ShowMessage("Please enter a path to your latest transactions:");
        string transactionsPath = _transactionUserInteraction.GetPath();

        Console.WriteLine($"Transaction path is {transactionsPath}");
    }
}