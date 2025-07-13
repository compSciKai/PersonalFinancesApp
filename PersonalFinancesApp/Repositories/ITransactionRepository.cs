using PersonalFinances.Models;

namespace PersonalFinances.Repositories;

public interface ITransactionsRepository
{
    List<T> GetTransactions<T>(string filePath);
    void ExportTransactions(List<Transaction> transactions, string filePath);
}

