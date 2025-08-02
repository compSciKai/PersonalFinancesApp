using Microsoft.EntityFrameworkCore;
using PersonalFinances.Data;
using PersonalFinances.Models;

namespace PersonalFinances.Repositories;
public class SqlServerTransactionRepository<T> : ITransactionRepository<T> where T : Transaction
{
    public readonly TransactionContext _context;

    public SqlServerTransactionRepository(TransactionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _context.Set<T>()
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<List<T>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _context.Set<T>()
            .Where(t => t.Date >= start && t.Date <= end)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<int> SaveAsync(List<T> transactions)
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

    public async Task<bool> ExistsAsync(string transactionHash)
    {
        return await _context.Set<T>()
            .AnyAsync(t => t.TransactionHash == transactionHash);
    }
}

