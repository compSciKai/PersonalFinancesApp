using PersonalFinances.Models;
namespace PersonalFinances.App;

public interface ITransactionsUserInteraction
{
    void ShowMessage(string message);
    void Exit();
    string GetInput();
    void OutputTransactions(List<Transaction> transactions, string tableName);
    KeyValuePair<string, string>? PromptForVendorKVP(string transactionData);
    KeyValuePair<string, string>? PromptForCategoryKVP(string description);
    string? PromptForProfileChoice(List<string> profileNames);
}
