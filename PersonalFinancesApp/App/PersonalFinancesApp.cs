using PersonalFinances.Repositories;
using PersonalFinances.Models;
namespace PersonalFinances.App;
class PersonalFinancesApp
{
    private readonly ITransactionsRepository _transactionRepository;
    private readonly ITransactionsUserInteraction _transactionUserInteraction;
    private readonly IVendorsService _vendorsService;

    public PersonalFinancesApp(ITransactionsRepository transactionRepository, ITransactionsUserInteraction transactionUserInteraction)
    {
        _transactionRepository = transactionRepository;
        _transactionUserInteraction = transactionUserInteraction;
    }

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

    public void Run(string newTransactionsFilePath)
    {
        Console.WriteLine("Finances App Initialized");

        List<Transaction> rawTransactions = _transactionRepository.GetTransactions(newTransactionsFilePath);
        _transactionUserInteraction.OutputTransactions(rawTransactions);

        // TODO: add vendor to each transaction. Build up vendor list
            /*
                If no vendor: 
                    use regex method to see if vendor can be found (*)

                    if no vendor found from regex method:
                        Ask user to add a vendor for the entry
                        Save the vendor entry

                Create vendorService: saves vendors and regular expressions for each

                (*) Regex method:
                    take description and split on space characters
                    pass tokens into dictionary until key found
                        - if no key found, ask user to manually enter vender
            */
        // TODO: categorize
    }


    public void Run()
    {
        _transactionUserInteraction.ShowMessage("Please enter a path to your latest transactions:");
        string transactionsPath = _transactionUserInteraction.GetPath();

        Console.WriteLine($"Transaction path is {transactionsPath}");

        // TODO: complete method call this(transactionPath?)
    }
}