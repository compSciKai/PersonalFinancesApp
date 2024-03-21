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
            Console.WriteLine($"{transaction.Date}: {transaction.Vendor} - {transaction.Description} - {transaction.Amount}");
        }
    }

    public string PromptForVendorValue(string description)
    {
        bool invalidVendorInput = true;
        string input = "";
        ShowMessage(
        $"Vendor could not be found for this transaction with description: {description}.");

        while (invalidVendorInput)
        {
            ShowMessage("Enter a new vendor:");
            input = GetInput();

            if (input is not "" && description.ToLower().Contains(input.ToLower()))
            {
                invalidVendorInput = false;
            }
            else 
            {
                ShowMessage("That vendor is not present in the description. Try again.");
            }
        }

        return input;
    }
}
