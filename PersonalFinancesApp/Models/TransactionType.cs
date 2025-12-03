namespace PersonalFinances.Models;

/// <summary>
/// Represents the classification of a transaction for budgeting and reporting purposes.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Money received (salary, refunds, reimbursements, deposits).
    /// Typically shown as negative amounts in bank statements.
    /// Does not count against budget.
    /// </summary>
    Income = 1,

    /// <summary>
    /// Regular spending that counts against budget categories.
    /// This is the default type for most transactions.
    /// </summary>
    Expense = 2,

    /// <summary>
    /// Movement of money between your own accounts (e.g., credit card payments,
    /// account transfers, loan payments, TFSA/RRSP contributions).
    /// Does not count against budget.
    /// </summary>
    Transfer = 3,

    /// <summary>
    /// Bank fees, interest charges, rewards, refunds, or corrections.
    /// Tracked for visibility but typically not budgeted.
    /// Does not count against budget by default.
    /// </summary>
    Adjustment = 4
}
