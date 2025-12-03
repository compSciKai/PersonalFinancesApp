using PersonalFinances.Models;
using PersonalFinances.Repositories;
using static PersonalFinances.App.TransactionFilterService;

namespace PersonalFinances.App;

/// <summary>
/// Service for reprocessing transactions that have uninitialized types (Type=0).
/// </summary>
public class TransactionReprocessingService : ITransactionReprocessingService
{
    private readonly ITransactionRepository<RBCTransaction> _rbcRepository;
    private readonly ITransactionRepository<AmexTransaction> _amexRepository;
    private readonly ITransactionRepository<PCFinancialTransaction> _pcRepository;
    private readonly IVendorsService _vendorsService;
    private readonly ICategoriesService _categoriesService;
    private readonly IBudgetService _budgetService;

    public TransactionReprocessingService(
        ITransactionRepository<RBCTransaction> rbcRepository,
        ITransactionRepository<AmexTransaction> amexRepository,
        ITransactionRepository<PCFinancialTransaction> pcRepository,
        IVendorsService vendorsService,
        ICategoriesService categoriesService,
        IBudgetService budgetService)
    {
        _rbcRepository = rbcRepository;
        _amexRepository = amexRepository;
        _pcRepository = pcRepository;
        _vendorsService = vendorsService;
        _categoriesService = categoriesService;
        _budgetService = budgetService;
    }

    /// <summary>
    /// Gets all transactions with Type=0 (unprocessed) across all transaction types,
    /// filtered by the specified date range and user.
    /// </summary>
    public async Task<List<Transaction>> GetUnprocessedTransactionsAsync(TransactionRange? transactionRange, string? userName)
    {
        var rbcTransactions = await _rbcRepository.GetAllAsync();
        var amexTransactions = await _amexRepository.GetAllAsync();
        var pcTransactions = await _pcRepository.GetAllAsync();

        var allTransactions = new List<Transaction>();
        allTransactions.AddRange(rbcTransactions);
        allTransactions.AddRange(amexTransactions);
        allTransactions.AddRange(pcTransactions);

        // Filter by date range first
        var transactionsInRange = TransactionFilterService.GetTransactionsInRange(allTransactions, transactionRange);

        // Filter by user (if applicable)
        var filteredTransactions = transactionsInRange;
        if (userName != null)
        {
            filteredTransactions = TransactionFilterService.GetTransactionsForUser(transactionsInRange, userName);
        }

        // Then filter for unprocessed (Type=0)
        return filteredTransactions
            .Where(t => t.Type == 0 || t.Type == default(TransactionType))
            .OrderBy(t => t.Date)
            .ToList();
    }

    /// <summary>
    /// Reprocesses the given transactions through the type detection and categorization flow.
    /// Updates the database with the newly assigned types and categories.
    /// </summary>
    public async Task<int> ReprocessTransactionsAsync(List<Transaction> transactions, BudgetProfile profile)
    {
        if (!transactions.Any())
            return 0;

        Console.WriteLine($"\nReprocessing {transactions.Count} transaction(s)...\n");

        // Step 1: Add vendors
        var transactionsWithVendors = await _vendorsService.AddVendorsToTransactionsAsync(transactions);

        // Step 2: Add categories and detect types
        var transactionsWithCategories = await _categoriesService.AddCategoriesToTransactionsAsync(
            transactionsWithVendors,
            profile,
            _budgetService);

        // Step 3: Persist changes to database
        var rbcToUpdate = transactionsWithCategories.OfType<RBCTransaction>().ToList();
        var amexToUpdate = transactionsWithCategories.OfType<AmexTransaction>().ToList();
        var pcToUpdate = transactionsWithCategories.OfType<PCFinancialTransaction>().ToList();

        int updatedCount = 0;

        if (rbcToUpdate.Any())
            updatedCount += await _rbcRepository.UpdateAsync(rbcToUpdate);
        if (amexToUpdate.Any())
            updatedCount += await _amexRepository.UpdateAsync(amexToUpdate);
        if (pcToUpdate.Any())
            updatedCount += await _pcRepository.UpdateAsync(pcToUpdate);

        Console.WriteLine($"\n✓ Successfully reprocessed {updatedCount} transaction(s).\n");

        return updatedCount;
    }
}
