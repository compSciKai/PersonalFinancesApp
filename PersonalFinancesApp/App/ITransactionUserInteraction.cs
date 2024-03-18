namespace PersonalFinances.App;

public interface ITransactionsUserInteraction
{
    void ShowMessage(string message);
    void Exit();
    string GetPath();
    // void PrintExistingTransactions(IEnumerable<Transaction> allTransactions);
}
