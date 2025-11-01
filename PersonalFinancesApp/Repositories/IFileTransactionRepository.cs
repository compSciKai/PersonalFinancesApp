using PersonalFinances.Models;

namespace PersonalFinances.Repositories;

public interface IFileTransactionRepository<T> : ITransactionRepository<T> where T : Transaction
{
    Task<List<T>> LoadFromFileAsync(string filePath);
    Task SaveToFileAsync(List<T> transactions, string filePath);
}