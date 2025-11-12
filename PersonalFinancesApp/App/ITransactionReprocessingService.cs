using PersonalFinances.Models;
using static PersonalFinances.App.TransactionFilterService;

namespace PersonalFinances.App;

/// <summary>
/// Service for reprocessing transactions that have uninitialized types (Type=0).
/// </summary>
public interface ITransactionReprocessingService
{
    /// <summary>
    /// Gets all transactions with Type=0 (unprocessed) across all transaction types,
    /// filtered by the specified date range and user.
    /// </summary>
    /// <param name="transactionRange">Date range filter to apply.</param>
    /// <param name="userName">User name to filter transactions by (null for all users).</param>
    /// <returns>List of unprocessed transactions from all repositories within the date range for the specified user.</returns>
    Task<List<Transaction>> GetUnprocessedTransactionsAsync(TransactionRange? transactionRange, string? userName);

    /// <summary>
    /// Reprocesses the given transactions through the type detection and categorization flow.
    /// Updates the database with the newly assigned types and categories.
    /// </summary>
    /// <param name="transactions">Transactions to reprocess.</param>
    /// <param name="profile">Budget profile to use for categorization.</param>
    /// <returns>Count of successfully reprocessed transactions.</returns>
    Task<int> ReprocessTransactionsAsync(List<Transaction> transactions, BudgetProfile profile);
}
