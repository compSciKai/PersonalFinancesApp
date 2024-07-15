using PersonalFinances.Models;

namespace PersonalFinances.Repositories;

public interface ITransactionsRepository
{
    List<Transaction> GetTransactions(string filePath);
    void ExportTransactions(List<Transaction> transactions, string filePath);
}
