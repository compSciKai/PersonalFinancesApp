using PersonalFinances.Models;

namespace PersonalFinances.App;

public class TransactionsConsoleUserInteraction : ITransactionsUserInteraction
{
    public TransactionsConsoleUserInteraction()
    {
    }

    public void ShowMessage(string message) 
    {
        Console.WriteLine(message);
    }

    public string GetInput() 
    {
        string input = Console.ReadLine();
        return input;
    }

    public void Exit() 
    {
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }

    public void OutputTransactions(IEnumerable<Transaction> transactions)
    {
        foreach (Transaction transaction in transactions)
        {
            Console.WriteLine($"{transaction.Date.ToShortDateString()}: {transaction.Vendor} - {transaction.Description} - {transaction.Amount}");
        }
    }

    public KeyValuePair<string, string>? PromptForVendorKVP(string description)
    {
        bool invalidVendorInput = true;
        string vendorKey = ""; 
        string vendorValue = "";

        ShowMessage(
        $"Vendor could not be found for this transaction with description: '{description}'.");

        while (invalidVendorInput)
        {
            ShowMessage("Enter a string from the description that will identify the vendor for this transaction, or type 's' to skip");
            vendorKey = GetInput();
            
            if (vendorKey == "s")
            {
                return null;
            }

            if (vendorKey is not "" && description.ToLower().Contains(vendorKey.ToLower()))
            {
                ShowMessage($"What is the vendor's name for this transaction? Press enter to save as '{vendorKey}'");
                vendorValue = GetInput();

                if (vendorValue is "")
                {
                    vendorValue = vendorKey;
                }

                invalidVendorInput = false; 
            }
            else 
            {
                ShowMessage("That vendor is not present in the description. Try again.");
            }
        }

        return new KeyValuePair<string, string>(vendorKey.ToLower(), vendorValue.ToLower());
    }
}
