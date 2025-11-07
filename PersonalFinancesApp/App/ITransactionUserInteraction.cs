using PersonalFinances.Models;
namespace PersonalFinances.App;

public interface ITransactionsUserInteraction
{
    void ShowMessage(string message);
    void Exit();
    string GetInput();
    void OutputTransactions(List<Transaction> transactions, string tableName, BudgetProfile? profile);
    (KeyValuePair<string, string>? kvp, bool skipAll) PromptForVendorKVP(string transactionData);
    (KeyValuePair<string, string>? kvp, bool skipAll, bool addToBudget) PromptForCategoryKVP(string vendor, BudgetProfile? profile);
    string? PromptForProfileChoice(List<string> profileNames);
    void OutputBudgetVsActual(List<Transaction> transactions, BudgetProfile? profile);
    double PromptForBudgetAmount(string categoryName, double remainingBudget);
}
