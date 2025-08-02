using Microsoft.EntityFrameworkCore;
using PersonalFinances.Data;
using PersonalFinances.Models;

namespace PersonalFinances.Repositories;
public class SqlServerTransactionRepository : ITransactionsRepository, ITransactionSavable
{
    public readonly TransactionContext _context;

    public SqlServerTransactionRepository(TransactionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void ExportTransactions(List<Transaction> transactions, string filePath)
    {
        throw new NotImplementedException();
    }

    public async Task<List<T>> GetTransactionsAsync<T>() where T : Transaction
    {
        return await _context.Set<T>()
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public List<T> GetTransactions<T>() where T : Transaction
    {
        return GetTransactionsAsync<T>().GetAwaiter().GetResult();
    }

    public async Task<int> SaveTransactionsWithHashAsync<T>(List<T> transactions) where T : Transaction
    {
        // Generate hashes for all transactions
        foreach (var transaction in transactions)
        {
            transaction.GenerateHash();
        }

        // Get existing hashes from database
        var existingHashes = await _context.Set<T>()
            .Select(t => t.TransactionHash)
            .ToHashSetAsync();

        // Filter out duplicates
        var newTransactions = transactions
            .Where(t => !existingHashes.Contains(t.TransactionHash))
            .ToList();

        if (newTransactions.Any())
        {
            _context.Set<T>().AddRange(newTransactions);
            await _context.SaveChangesAsync();
        }

        return newTransactions.Count;
    }
}

