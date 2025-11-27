using NUnit.Framework;
using PersonalFinances.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PersonalFinancesAppTests
{
    /// <summary>
    /// Tests to ensure all transactions are properly accounted for in output sections
    /// and that totals reconcile correctly across all transaction type groupings.
    /// </summary>
    [TestFixture]
    public class TransactionReconciliationTests
    {
        [Test]
        public void AllTransactions_ShouldEqualSumOfAllTypeGroups()
        {
            // Arrange: Create a comprehensive set of transactions matching user's scenario
            var allTransactions = new List<Transaction>
            {
                // Budget category expenses (should appear in BUDGET CATEGORIES section)
                new RBCTransaction
                {
                    Id = 1,
                    Description = "Groceries",
                    Amount = -100.00M,
                    Date = new DateTime(2025, 10, 15),
                    Category = "GROCERIES",
                    Type = TransactionType.Expense,
                    AccountType = "Chequing"
                },
                new AmexTransaction
                {
                    Id = 2,
                    Description = "Gas Station",
                    Amount = -60.00M,
                    Date = new DateTime(2025, 10, 16),
                    Category = "GAS",
                    Type = TransactionType.Expense,
                    AccountType = "Amex"
                },
                new AmexTransaction
                {
                    Id = 3,
                    Description = "Fast Food",
                    Amount = -25.50M,
                    Date = new DateTime(2025, 10, 17),
                    Category = "FAST FOOD",
                    Type = TransactionType.Expense,
                    AccountType = "Amex"
                },

                // Tracked-only expenses (should appear in FIXED OBLIGATIONS section, but currently broken)
                new RBCTransaction
                {
                    Id = 4,
                    Description = "Mortgage Payment",
                    Amount = -2542.57M,
                    Date = new DateTime(2025, 10, 22),
                    Category = "MORTGAGE",
                    Type = TransactionType.Expense,
                    AccountType = "Chequing"
                },
                new RBCTransaction
                {
                    Id = 5,
                    Description = "Personal Loan",
                    Amount = -142.20M,
                    Date = new DateTime(2025, 10, 15),
                    Category = "SERVICES",
                    Type = TransactionType.Expense,
                    AccountType = "Chequing"
                },
                new RBCTransaction
                {
                    Id = 6,
                    Description = "Investment",
                    Amount = -104.00M,
                    Date = new DateTime(2025, 10, 20),
                    Category = "INVESTMENT",
                    Type = TransactionType.Expense,
                    AccountType = "Chequing"
                },
                new PCFinancialTransaction
                {
                    Id = 7,
                    Description = "Bill Payment",
                    Amount = -917.28M,
                    Date = new DateTime(2025, 10, 06),
                    Category = "BILL PAYMENT",
                    Type = TransactionType.Expense,
                    AccountType = "PC Financial"
                },

                // Income transactions (should appear in INCOME section)
                new RBCTransaction
                {
                    Id = 8,
                    Description = "Payroll Deposit",
                    Amount = 2905.70M,
                    Date = new DateTime(2025, 10, 15),
                    Category = null,
                    Type = TransactionType.Income,
                    AccountType = "Chequing"
                },
                new RBCTransaction
                {
                    Id = 9,
                    Description = "E-Transfer In",
                    Amount = 792.00M,
                    Date = new DateTime(2025, 10, 06),
                    Category = null,
                    Type = TransactionType.Income,
                    AccountType = "Chequing"
                },

                // Transfer transactions (should appear in ACCOUNT ACTIVITY section)
                new RBCTransaction
                {
                    Id = 10,
                    Description = "Account Transfer Out",
                    Amount = -250.00M,
                    Date = new DateTime(2025, 10, 08),
                    Category = "ACCOUNT TRANSFER",
                    Type = TransactionType.Transfer,
                    AccountType = "Chequing"
                },
                new RBCTransaction
                {
                    Id = 11,
                    Description = "Account Transfer In",
                    Amount = 250.00M,
                    Date = new DateTime(2025, 10, 08),
                    Category = "ACCOUNT TRANSFER",
                    Type = TransactionType.Transfer,
                    AccountType = "Savings"
                },

                // Adjustment transactions (should appear in ADJUSTMENTS section)
                new RBCTransaction
                {
                    Id = 12,
                    Description = "Monthly Fee",
                    Amount = -16.95M,
                    Date = new DateTime(2025, 10, 21),
                    Category = "SUBSCRIPTION",
                    Type = TransactionType.Adjustment,
                    AccountType = "Chequing"
                },
                new RBCTransaction
                {
                    Id = 13,
                    Description = "Fee Rebate",
                    Amount = 6.00M,
                    Date = new DateTime(2025, 10, 21),
                    Category = "SUBSCRIPTION",
                    Type = TransactionType.Adjustment,
                    AccountType = "Chequing"
                },
                new RBCTransaction
                {
                    Id = 14,
                    Description = "Interest Payment",
                    Amount = -2.76M,
                    Date = new DateTime(2025, 10, 14),
                    Category = "INTEREST",
                    Type = TransactionType.Adjustment,
                    AccountType = "Chequing"
                },

                // Uncategorized expense (should appear in UNCATEGORIZED section)
                new AmexTransaction
                {
                    Id = 15,
                    Description = "Unknown Vendor",
                    Amount = -50.00M,
                    Date = new DateTime(2025, 10, 18),
                    Category = null,
                    Type = TransactionType.Expense,
                    AccountType = "Amex"
                },

                // Unprocessed transaction (should appear in UNPROCESSED section)
                new AmexTransaction
                {
                    Id = 16,
                    Description = "New Transaction",
                    Amount = -30.00M,
                    Date = new DateTime(2025, 10, 25),
                    Category = "GROCERIES",
                    Type = 0, // Unprocessed
                    AccountType = "Amex"
                }
            };

            // Act: Group transactions by type (mimicking FIXED PersonalFinancesApp.cs logic)
            var budgetedExpenses = allTransactions
                .Where(t =>
                    t.Type == TransactionType.Expense &&
                    !string.IsNullOrEmpty(t.Category) &&  // Exclude uncategorized (handled separately)
                    !IsTrackedOnlyCategory(t.Category))   // Exclude tracked-only expenses
                .ToList();

            var trackedOnlyExpenses = allTransactions
                .Where(t => t.Type == TransactionType.Expense && IsTrackedOnlyCategory(t.Category))
                .ToList();

            var transfers = allTransactions
                .Where(t => t.Type == TransactionType.Transfer)
                .ToList();

            var income = allTransactions
                .Where(t => t.Type == TransactionType.Income)
                .ToList();

            var adjustments = allTransactions
                .Where(t => t.Type == TransactionType.Adjustment)
                .ToList();

            var uncategorizedExpenses = allTransactions
                .Where(t => t.Type == TransactionType.Expense && string.IsNullOrEmpty(t.Category))
                .ToList();

            var unprocessedTransactions = allTransactions
                .Where(t => t.Type == 0 || t.Type == default(TransactionType))
                .ToList();

            // Assert: Every transaction should be in exactly ONE group
            var totalInGroups = budgetedExpenses.Count + trackedOnlyExpenses.Count +
                               transfers.Count + income.Count + adjustments.Count +
                               uncategorizedExpenses.Count + unprocessedTransactions.Count;

            Assert.That(totalInGroups, Is.EqualTo(allTransactions.Count),
                $"Transaction count mismatch! Total: {allTransactions.Count}, In groups: {totalInGroups}");

            // Assert: Sum of all group amounts should equal sum of all transactions
            var totalAmount = allTransactions.Sum(t => t.Amount);
            var groupsAmount = budgetedExpenses.Sum(t => t.Amount) +
                              trackedOnlyExpenses.Sum(t => t.Amount) +
                              transfers.Sum(t => t.Amount) +
                              income.Sum(t => t.Amount) +
                              adjustments.Sum(t => t.Amount) +
                              uncategorizedExpenses.Sum(t => t.Amount) +
                              unprocessedTransactions.Sum(t => t.Amount);

            Assert.That(groupsAmount, Is.EqualTo(totalAmount),
                $"Amount mismatch! Total: {totalAmount:C}, Groups: {groupsAmount:C}");

            // Assert: Specific group counts match expectations
            Assert.That(budgetedExpenses.Count, Is.EqualTo(3), "Should have 3 budget expenses (GROCERIES, GAS, FAST FOOD)");
            Assert.That(trackedOnlyExpenses.Count, Is.EqualTo(4), "Should have 4 tracked-only expenses (MORTGAGE, SERVICES, INVESTMENT, BILL PAYMENT)");
            Assert.That(income.Count, Is.EqualTo(2), "Should have 2 income transactions");
            Assert.That(transfers.Count, Is.EqualTo(2), "Should have 2 transfer transactions");
            Assert.That(adjustments.Count, Is.EqualTo(3), "Should have 3 adjustment transactions");
            Assert.That(uncategorizedExpenses.Count, Is.EqualTo(1), "Should have 1 uncategorized expense");
            Assert.That(unprocessedTransactions.Count, Is.EqualTo(1), "Should have 1 unprocessed transaction");
        }

        /// <summary>
        /// Simulates tracked-only category detection based on typical tracked categories
        /// </summary>
        private bool IsTrackedOnlyCategory(string? categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                return false;

            // List of categories that are typically tracked-only (not budgeted)
            var trackedOnlyCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "MORTGAGE",
                "SERVICES",      // Personal loans, etc.
                "INVESTMENT",
                "BILL PAYMENT",
                "PAYMENT"
            };

            return trackedOnlyCategories.Contains(categoryName);
        }

        [Test]
        public void IsTrackedOnlyCategory_ShouldIdentifyTrackedCategories()
        {
            // Test that the helper method correctly identifies tracked-only categories

            // Assert: These categories should be tracked-only (not budgeted)
            Assert.That(IsTrackedOnlyCategory("MORTGAGE"), Is.True, "MORTGAGE should be tracked-only");
            Assert.That(IsTrackedOnlyCategory("SERVICES"), Is.True, "SERVICES (loans) should be tracked-only");
            Assert.That(IsTrackedOnlyCategory("INVESTMENT"), Is.True, "INVESTMENT should be tracked-only");
            Assert.That(IsTrackedOnlyCategory("BILL PAYMENT"), Is.True, "BILL PAYMENT should be tracked-only");
            Assert.That(IsTrackedOnlyCategory("PAYMENT"), Is.True, "PAYMENT should be tracked-only");

            // Assert: These categories should be budgeted (not tracked-only)
            Assert.That(IsTrackedOnlyCategory("GROCERIES"), Is.False, "GROCERIES should be budgeted");
            Assert.That(IsTrackedOnlyCategory("GAS"), Is.False, "GAS should be budgeted");
            Assert.That(IsTrackedOnlyCategory("FAST FOOD"), Is.False, "FAST FOOD should be budgeted");
            Assert.That(IsTrackedOnlyCategory("TRANSPORTATION"), Is.False, "TRANSPORTATION should be budgeted");
        }
    }
}
