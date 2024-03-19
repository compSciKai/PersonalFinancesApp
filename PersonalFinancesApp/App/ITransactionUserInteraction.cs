using PersonalFinances.Models;
namespace PersonalFinances.App;

public interface ITransactionsUserInteraction
{
    void ShowMessage(string message);
    void Exit();
    string GetPath();
    void OutputTransactions(IEnumerable<Transaction> transactions);
}
