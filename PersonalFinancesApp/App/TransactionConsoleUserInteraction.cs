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

    public string GetPath() 
    {
        string input = Console.ReadLine();
        return input;
    }

    public void Exit() 
    {
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }
}
