using PersonalFinances.Models;

namespace PersonalFinances.Repositories;

public interface ITransactionRepository<T> where T : Transaction
{
    Task<List<T>> GetAllAsync();
    Task<List<T>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task<int> SaveAsync(List<T> transactions);
    Task<bool> ExistsAsync(string transactionHash);
}