using PersonalFinances.Models;

namespace PersonalFinances.App;

/// <summary>
/// Service for auto-detecting transaction types based on configurable rules.
/// </summary>
public class TransactionTypeDetector
{
    private readonly string[] _transferKeywords = new[]
    {
        "transfer", "e-transfer", "etransfer", "e-trf", "interac",
        "withdrawal", "deposit to", "payment to account",
        "tfsa", "rrsp", "loan payment", "mortgage", "credit payment"
    };

    private readonly string[] _nonBudgetedKeywords = new[]
    {
        "fee", "interest charge", "overdraft",
        "cashback", "reward", "refund", "reversal"
    };

    private readonly string[] _incomeKeywords = new[]
    {
        "income", "salary", "ei", "ei canada", "employment insurance",
        "tax refund", "deposit", "reimbursement", "payroll"
    };

    /// <summary>
    /// Detects the transaction type using a priority-based rule system.
    /// </summary>
    /// <param name="transaction">The transaction to classify</param>
    /// <param name="vendorMapping">Optional vendor mapping with type hints</param>
    /// <param name="category">Optional category object</param>
    /// <returns>Detected transaction type</returns>
    public TransactionType DetectType(
        Transaction transaction,
        VendorMapping? vendorMapping = null,
        Category? category = null)
    {
        // PRIORITY 1: Vendor Override - User explicitly set this type
        if (vendorMapping?.OverrideType == true && vendorMapping.SuggestedType.HasValue)
        {
            return vendorMapping.SuggestedType.Value;
        }

        // PRIORITY 2: Vendor Suggestion (not forced)
        if (vendorMapping?.SuggestedType.HasValue == true && !vendorMapping.OverrideType)
        {
            // Treat as a strong hint but can be overridden by other rules
            // For now, we'll use it if no other signals contradict it
            // This could be enhanced with confidence scoring
        }

        // PRIORITY 3: Amount-based detection (negative = income in most bank CSVs)
        if (transaction.Amount < 0)
        {
            return TransactionType.Income;
        }

        // PRIORITY 4: Keyword-based detection
        var lowerDescription = transaction.Description?.ToLower() ?? string.Empty;
        var lowerVendor = transaction.Vendor?.ToLower() ?? string.Empty;
        var lowerCategory = transaction.Category?.ToLower() ?? string.Empty;

        // Check for transfer keywords
        if (ContainsAnyKeyword(lowerDescription, _transferKeywords) ||
            ContainsAnyKeyword(lowerVendor, _transferKeywords) ||
            ContainsAnyKeyword(lowerCategory, _transferKeywords))
        {
            return TransactionType.Transfer;
        }

        // Check for income keywords (in case amount is positive but it's still income)
        if (ContainsAnyKeyword(lowerDescription, _incomeKeywords) ||
            ContainsAnyKeyword(lowerVendor, _incomeKeywords) ||
            ContainsAnyKeyword(lowerCategory, _incomeKeywords))
        {
            return TransactionType.Income;
        }

        // Check for non-budgeted keywords
        if (ContainsAnyKeyword(lowerDescription, _nonBudgetedKeywords) ||
            ContainsAnyKeyword(lowerVendor, _nonBudgetedKeywords))
        {
            return TransactionType.NonBudgeted;
        }

        // PRIORITY 5: Use vendor suggestion if we haven't matched anything else
        if (vendorMapping?.SuggestedType.HasValue == true)
        {
            return vendorMapping.SuggestedType.Value;
        }

        // PRIORITY 6: Default to Expense
        return TransactionType.Expense;
    }

    /// <summary>
    /// Checks if the text contains any of the provided keywords.
    /// </summary>
    private bool ContainsAnyKeyword(string text, string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a confidence score for the detected type (0.0 to 1.0).
    /// Higher score means more confident in the detection.
    /// </summary>
    public double GetConfidence(
        Transaction transaction,
        VendorMapping? vendorMapping,
        TransactionType detectedType)
    {
        double confidence = 0.5; // Base confidence

        // High confidence if vendor override is set
        if (vendorMapping?.OverrideType == true && vendorMapping.SuggestedType == detectedType)
        {
            return 1.0;
        }

        // Amount-based detection is highly reliable for income
        if (transaction.Amount < 0 && detectedType == TransactionType.Income)
        {
            confidence = 0.9;
        }

        // Keyword matches increase confidence
        var lowerDescription = transaction.Description?.ToLower() ?? string.Empty;
        var lowerVendor = transaction.Vendor?.ToLower() ?? string.Empty;
        var lowerCategory = transaction.Category?.ToLower() ?? string.Empty;

        var keywordMatch = detectedType switch
        {
            TransactionType.Transfer => ContainsAnyKeyword(lowerDescription, _transferKeywords) ||
                                       ContainsAnyKeyword(lowerVendor, _transferKeywords),
            TransactionType.Income => ContainsAnyKeyword(lowerDescription, _incomeKeywords) ||
                                     ContainsAnyKeyword(lowerVendor, _incomeKeywords),
            TransactionType.NonBudgeted => ContainsAnyKeyword(lowerDescription, _nonBudgetedKeywords) ||
                                          ContainsAnyKeyword(lowerVendor, _nonBudgetedKeywords),
            _ => false
        };

        if (keywordMatch)
        {
            confidence = Math.Max(confidence, 0.8);
        }

        // Vendor suggestion (without override) provides moderate confidence
        if (vendorMapping?.SuggestedType == detectedType && !vendorMapping.OverrideType)
        {
            confidence = Math.Max(confidence, 0.7);
        }

        return confidence;
    }
}
