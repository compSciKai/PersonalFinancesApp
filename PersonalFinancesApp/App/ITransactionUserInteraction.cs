using PersonalFinances.Models;
namespace PersonalFinances.App;

public interface ITransactionsUserInteraction
{
    void ShowMessage(string message);
    void Exit();
    string GetInput();
    void OutputTransactions(List<Transaction> transactions, string tableName, BudgetProfile? profile);
    (KeyValuePair<string, string>? kvp, bool skipAll) PromptForVendorKVP(string transactionData);
    (KeyValuePair<string, string>? kvp, bool skipAll) PromptForCategoryKVP(string description);
    string? PromptForProfileChoice(List<string> profileNames);
    void OutputBudgetVsActual(List<Transaction> transactions, BudgetProfile? profile);
}
