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
        if (!transactions.Any())
            return 0;

        // Generate hashes for all transactions
        foreach (var transaction in transactions)
        {
            transaction.GenerateHash();
        }

        // Deduplicate within the incoming batch (take first occurrence of each hash)
        var uniqueTransactions = transactions
            .GroupBy(t => t.TransactionHash)
            .Select(g => g.First())
            .ToList();

        // Get all hashes from the deduplicated transactions
        var incomingHashes = uniqueTransactions.Select(t => t.TransactionHash).ToList();

        // Batch check existing hashes in database using IN clause
        var existingHashes = await _context.Set<T>()
            .Where(t => incomingHashes.Contains(t.TransactionHash))
            .Select(t => t.TransactionHash)
            .ToHashSetAsync();

        // Filter out transactions that already exist in database
        var newTransactions = uniqueTransactions
            .Where(t => !existingHashes.Contains(t.TransactionHash))
            .ToList();

        if (newTransactions.Any())
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Set<T>().AddRange(newTransactions);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        return newTransactions.Count;
    }

    public async Task<bool> ExistsAsync(string transactionHash)
    {
        return await _context.Set<T>()
            .AnyAsync(t => t.TransactionHash == transactionHash);
    }
}

