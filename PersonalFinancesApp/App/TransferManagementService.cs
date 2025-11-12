using PersonalFinances.Models;
using PersonalFinances.Repositories;

namespace PersonalFinances.App;

public class TransferManagementService : ITransferManagementService
{
    private readonly ITransactionsUserInteraction _userInteraction;
    private readonly ITransactionRepository<RBCTransaction> _rbcRepository;
    private readonly ITransactionRepository<AmexTransaction> _amexRepository;
    private readonly ITransactionRepository<PCFinancialTransaction> _pcRepository;
    private readonly ICategoriesService _categoriesService;

    public TransferManagementService(
        ITransactionsUserInteraction userInteraction,
        ITransactionRepository<RBCTransaction> rbcRepository,
        ITransactionRepository<AmexTransaction> amexRepository,
        ITransactionRepository<PCFinancialTransaction> pcRepository,
        ICategoriesService categoriesService)
    {
        _userInteraction = userInteraction;
        _rbcRepository = rbcRepository;
        _amexRepository = amexRepository;
        _pcRepository = pcRepository;
        _categoriesService = categoriesService;
    }

    /// <summary>
    /// Main method to manage transfers: E-transfer review, matching, and reclassification
    /// </summary>
    public async Task ManageTransfersAsync(List<Transaction> transactions, BudgetProfile profile)
    {
        Console.WriteLine("\n═══════════════════════════════════════════════════════════");
        Console.WriteLine("                    TRANSFER MANAGEMENT");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        // Step 1: E-Transfer Review
        await ReviewETransfersAsync(transactions);

        // Step 2: Automatic Transfer Matching
        await MatchTransfersAsync(transactions);

        // Step 3: Unmatched Transfer Review
        await ReviewUnmatchedTransfersAsync(transactions, profile);

        // Persist all changes
        await PersistTransactionChangesAsync(transactions);

        Console.WriteLine("\n✓ Transfer management complete.\n");
    }

    /// <summary>
    /// Step 1: Review E-Transfers and allow reclassification
    /// </summary>
    private async Task ReviewETransfersAsync(List<Transaction> transactions)
    {
        var eTransfers = transactions
            .Where(t => t.Type == TransactionType.Transfer &&
                       (t.Description.ToLower().Contains("e-trf") ||
                        t.Description.ToLower().Contains("interac")))
            .ToList();

        if (!eTransfers.Any())
        {
            Console.WriteLine("No E-Transfers found.\n");
            return;
        }

        Console.WriteLine($"Found {eTransfers.Count} E-Transfer(s):\n");

        for (int i = 0; i < eTransfers.Count; i++)
        {
            var trans = eTransfers[i];
            var account = GetFormattedAccountInfo(trans);
            var direction = trans.Amount < 0 ? "↑ OUT" : "↓ IN ";
            Console.WriteLine($"[{i + 1}] {trans.Date:MMM dd}  {account,-20} {trans.Description,-40} ${Math.Abs(trans.Amount),10:N2} {direction}");
        }

        Console.WriteLine("\nOptions:");
        Console.WriteLine("  [#]  - Reclassify transaction # as Expense");
        Console.WriteLine("  [c]  - Continue to transfer matching");
        Console.WriteLine("  [s]  - Skip transfer management entirely");
        Console.Write("\nYour choice: ");

        var input = Console.ReadLine()?.Trim().ToLower();

        if (input == "s")
        {
            throw new OperationCanceledException("Transfer management skipped by user.");
        }

        while (input != "c" && input != "s")
        {
            if (int.TryParse(input, out int index) && index > 0 && index <= eTransfers.Count)
            {
                var transaction = eTransfers[index - 1];
                await ReclassifyAsExpenseAsync(transaction);
            }
            else if (!string.IsNullOrEmpty(input))
            {
                Console.WriteLine("Invalid option. Try again.");
            }

            Console.Write("\nYour choice ([#]/c/s): ");
            input = Console.ReadLine()?.Trim().ToLower();

            if (input == "s")
            {
                throw new OperationCanceledException("Transfer management skipped by user.");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Reclassify a transaction as an Expense and prompt for category
    /// </summary>
    private async Task ReclassifyAsExpenseAsync(Transaction transaction)
    {
        Console.Write($"\nReclassifying: {transaction.Description}\n");
        Console.Write("Enter category name: ");
        var category = Console.ReadLine()?.Trim();

        if (!string.IsNullOrEmpty(category))
        {
            transaction.Type = TransactionType.Expense;
            transaction.Category = category;
            Console.WriteLine($"✓ Reclassified as Expense → {category}");

            // Ask if they want to save this as a vendor mapping
            Console.Write("Apply this category to all future transactions from this vendor? (y/n): ");
            var saveMapping = Console.ReadLine()?.Trim().ToLower();

            if (saveMapping == "y" && !string.IsNullOrEmpty(transaction.Vendor))
            {
                await _categoriesService.StoreNewCategoryAsync(
                    transaction.Vendor,
                    category,
                    TransactionType.Expense,
                    overrideType: true,
                    isTrackedOnly: false);
                Console.WriteLine("✓ Vendor mapping saved.");
            }
        }
    }

    /// <summary>
    /// Step 2: Find and match potential transfer pairs
    /// </summary>
    private async Task MatchTransfersAsync(List<Transaction> transactions)
    {
        var transfers = transactions
            .Where(t => t.Type == TransactionType.Transfer && !t.IsReconciledTransfer)
            .ToList();

        if (!transfers.Any())
        {
            Console.WriteLine("No unmatched transfers to review.\n");
            return;
        }

        Console.WriteLine($"Searching for transfer matches among {transfers.Count} transfer(s)...\n");

        var matchedCount = 0;

        for (int i = 0; i < transfers.Count; i++)
        {
            var trans1 = transfers[i];

            // Skip if already matched
            if (trans1.IsReconciledTransfer)
                continue;

            var potentialMatches = FindPotentialMatches(trans1, transfers);

            if (potentialMatches.Any())
            {
                foreach (var match in potentialMatches)
                {
                    var trans2 = match.Transaction;
                    var confidence = match.Confidence;
                    var confidenceLabel = match.ConfidenceLabel;

                    var account1 = GetFormattedAccountInfo(trans1);
                    var account2 = GetFormattedAccountInfo(trans2);

                    Console.WriteLine($"Potential Match ({confidenceLabel} confidence):");
                    Console.WriteLine($"  {trans1.Date:MMM dd}  {account1,-20} {trans1.Description,-40} ${Math.Abs(trans1.Amount),10:N2} ↑ OUT");
                    Console.WriteLine($"  {trans2.Date:MMM dd}  {account2,-20} {trans2.Description,-40} ${Math.Abs(trans2.Amount),10:N2} ↓ IN");
                    Console.Write("\nLink these transactions? (y/n): ");

                    var confirm = Console.ReadLine()?.Trim().ToLower();

                    if (confirm == "y")
                    {
                        // Generate a unique ID for this transfer pair
                        var linkedId = Guid.NewGuid().ToString();
                        trans1.LinkedTransactionId = linkedId;
                        trans2.LinkedTransactionId = linkedId;
                        trans1.IsReconciledTransfer = true;
                        trans2.IsReconciledTransfer = true;

                        Console.WriteLine("✓ Transactions linked.\n");
                        matchedCount++;
                        break; // Move to next transaction
                    }
                    else
                    {
                        Console.WriteLine("Skipped.\n");
                    }
                }
            }
        }

        if (matchedCount > 0)
        {
            Console.WriteLine($"✓ Matched {matchedCount} transfer pair(s).\n");
        }
        else
        {
            Console.WriteLine("No transfers were matched.\n");
        }
    }

    /// <summary>
    /// Find potential matching transfers for a given transaction
    /// </summary>
    private List<TransferMatch> FindPotentialMatches(Transaction transaction, List<Transaction> allTransfers)
    {
        var matches = new List<TransferMatch>();

        foreach (var other in allTransfers)
        {
            // Skip same transaction or already reconciled
            if (other.Id == transaction.Id || other.IsReconciledTransfer)
                continue;

            var score = CalculateMatchScore(transaction, other);

            if (score >= 3) // Minimum threshold
            {
                var confidence = score >= 5 ? "High" : score >= 4 ? "Medium" : "Low";
                matches.Add(new TransferMatch
                {
                    Transaction = other,
                    Confidence = score,
                    ConfidenceLabel = confidence
                });
            }
        }

        // Return sorted by confidence (highest first)
        return matches.OrderByDescending(m => m.Confidence).ToList();
    }

    /// <summary>
    /// Calculate match confidence score
    /// </summary>
    private int CalculateMatchScore(Transaction t1, Transaction t2)
    {
        int score = 0;

        // Exact amount match (opposite signs)
        if (Math.Abs(Math.Abs(t1.Amount) - Math.Abs(t2.Amount)) < 0.01m &&
            ((t1.Amount > 0 && t2.Amount < 0) || (t1.Amount < 0 && t2.Amount > 0)))
        {
            score += 3;
        }

        // Date proximity
        var daysDiff = Math.Abs((t1.Date - t2.Date).TotalDays);
        if (daysDiff == 0)
            score += 2;
        else if (daysDiff <= 1)
            score += 1;

        // Different account types (likely transfer between accounts)
        if (t1.AccountType != t2.AccountType)
            score += 1;

        // Description similarity
        var desc1 = t1.Description.ToLower();
        var desc2 = t2.Description.ToLower();
        if ((desc1.Contains("transfer") || desc1.Contains("payment")) &&
            (desc2.Contains("transfer") || desc2.Contains("payment")))
        {
            score += 1;
        }

        return score;
    }

    /// <summary>
    /// Step 3: Review unmatched transfers and offer reclassification
    /// </summary>
    private async Task ReviewUnmatchedTransfersAsync(List<Transaction> transactions, BudgetProfile profile)
    {
        var unmatchedTransfers = transactions
            .Where(t => t.Type == TransactionType.Transfer && !t.IsReconciledTransfer)
            .ToList();

        if (!unmatchedTransfers.Any())
        {
            Console.WriteLine("All transfers have been matched or reclassified.\n");
            return;
        }

        Console.WriteLine($"Found {unmatchedTransfers.Count} unmatched transfer(s):\n");

        foreach (var trans in unmatchedTransfers)
        {
            var account = GetFormattedAccountInfo(trans);
            var direction = trans.Amount < 0 ? "↑ OUT" : "↓ IN ";
            Console.WriteLine($"\n{trans.Date:MMM dd}  {account,-20} {trans.Description,-40} ${Math.Abs(trans.Amount),10:N2} {direction}");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  [1] Reclassify as Expense (specify category)");
            Console.WriteLine("  [2] Reclassify as Income");
            Console.WriteLine("  [3] Reclassify as Adjustment");
            Console.WriteLine("  [4] Keep as Transfer");
            Console.Write("\nYour choice (1-4): ");

            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    await ReclassifyAsExpenseAsync(trans);
                    break;
                case "2":
                    trans.Type = TransactionType.Income;
                    trans.Category = null;
                    Console.WriteLine("✓ Reclassified as Income");
                    break;
                case "3":
                    trans.Type = TransactionType.Adjustment;
                    trans.Category = null;
                    Console.WriteLine("✓ Reclassified as Adjustment");
                    break;
                case "4":
                    Console.WriteLine("Kept as Transfer");
                    break;
                default:
                    Console.WriteLine("Invalid choice. Keeping as Transfer.");
                    break;
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Persist all transaction changes to database
    /// </summary>
    private async Task PersistTransactionChangesAsync(List<Transaction> transactions)
    {
        var rbcToUpdate = transactions.OfType<RBCTransaction>().ToList();
        var amexToUpdate = transactions.OfType<AmexTransaction>().ToList();
        var pcToUpdate = transactions.OfType<PCFinancialTransaction>().ToList();

        if (rbcToUpdate.Any())
            await _rbcRepository.UpdateAsync(rbcToUpdate);
        if (amexToUpdate.Any())
            await _amexRepository.UpdateAsync(amexToUpdate);
        if (pcToUpdate.Any())
            await _pcRepository.UpdateAsync(pcToUpdate);
    }

    /// <summary>
    /// Format account information with account number (last 4 digits) if available
    /// </summary>
    private string GetFormattedAccountInfo(Transaction transaction)
    {
        if (transaction is AmexTransaction amex && !string.IsNullOrEmpty(amex.AccountNumber))
        {
            // Get last 4 digits of account number
            var last4 = amex.AccountNumber.Length > 4
                ? amex.AccountNumber.Substring(amex.AccountNumber.Length - 4)
                : amex.AccountNumber;
            return $"{transaction.AccountType} (*{last4})";
        }

        return transaction.AccountType;
    }

    /// <summary>
    /// Helper class to store potential transfer matches with confidence
    /// </summary>
    private class TransferMatch
    {
        public Transaction Transaction { get; set; } = null!;
        public int Confidence { get; set; }
        public string ConfidenceLabel { get; set; } = null!;
    }
}
