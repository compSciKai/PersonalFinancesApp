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

    public void Run(string transactionsFilePath, string vendorsFilePath)
    {
        _vendorsService.Init(vendorsFilePath);
        Console.WriteLine("Finances App Initialized");

        // TODO: Validate paths

        List<Transaction> rawTransactions = _transactionRepository.GetTransactions(transactionsFilePath);

        foreach (var transaction in rawTransactions)
        {
            if (transaction.Vendor is null)
            {
                string? vendorName = _vendorsService.GetVendor(transaction.Description);

                if (vendorName == "")
                {
                    KeyValuePair<string, string>? vendorKVP = _transactionUserInteraction.PromptForVendorKVP(transaction.Description);
                    if (vendorKVP is null)
                    {
                        continue;
                    }

                    _vendorsService.StoreNewVendor(vendorsFilePath, vendorKVP?.Key, vendorKVP?.Value);
                    vendorName = vendorKVP?.Value;
                }

                transaction.Vendor = vendorName;
            }
        }

        _transactionUserInteraction.OutputTransactions(rawTransactions);

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
}