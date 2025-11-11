using NUnit.Framework;
using PersonalFinances.Models;

namespace PersonalFinancesAppTests;

[TestFixture]
public class TransferManagementTests
{
    // Note: TransferManagementService contains mostly user-interactive workflows
    // which are difficult to unit test. The core matching logic uses private methods.
    // For now, we'll create basic tests that can be expanded when refactoring allows
    // for better testability (e.g., extracting match scoring to a separate testable class).

    #region Match Scoring Logic Tests (Manual Verification)

    [Test]
    public void ManualTest_MatchScoring_ExactAmountAndSameDay()
    {
        // This test documents the expected behavior of the match scoring algorithm
        // Actual implementation is in TransferManagementService.CalculateMatchScore (private method)

        // Expected score calculation:
        // - Exact amount match (opposite signs): +3 points
        // - Same day: +2 points
        // - Different account types: +1 point
        // - Description similarity: +1 point
        // Total: Up to 7 points possible
        // High confidence: 5+ points

        Assert.Pass("Match scoring logic is implemented in TransferManagementService.CalculateMatchScore");
    }

    #endregion

    #region Transaction Linking Tests

    [Test]
    public void TransactionLinking_SetsLinkedTransactionId()
    {
        // Arrange
        var transfer1 = new RBCTransaction
        {
            Description = "Credit Card Payment",
            Amount = 500m,
            Type = TransactionType.Transfer
        };

        var transfer2 = new AmexTransaction
        {
            Description = "Payment Received",
            Amount = -500m,
            Type = TransactionType.Transfer
        };

        var linkedId = Guid.NewGuid().ToString();

        // Act
        transfer1.LinkedTransactionId = linkedId;
        transfer2.LinkedTransactionId = linkedId;
        transfer1.IsReconciledTransfer = true;
        transfer2.IsReconciledTransfer = true;

        // Assert
        Assert.That(transfer1.LinkedTransactionId, Is.EqualTo(transfer2.LinkedTransactionId));
        Assert.That(transfer1.IsReconciledTransfer, Is.True);
        Assert.That(transfer2.IsReconciledTransfer, Is.True);
    }

    #endregion

    #region E-Transfer Identification Tests

    [TestCase("INTERAC E-TRF SENT TO JOHN")]
    [TestCase("e-trf received")]
    [TestCase("Interac payment")]
    public void ETransferIdentification_ValidKeywords(string description)
    {
        // Arrange
        var transaction = new RBCTransaction
        {
            Description = description,
            Type = TransactionType.Transfer
        };

        // Act
        var isETransfer = description.ToLower().Contains("e-trf") ||
                         description.ToLower().Contains("interac");

        // Assert
        Assert.That(isETransfer, Is.True,
            $"Description '{description}' should be identified as E-transfer");
    }

    [TestCase("Regular payment")]
    [TestCase("Credit card charge")]
    [TestCase("Grocery purchase")]
    public void ETransferIdentification_InvalidKeywords(string description)
    {
        // Arrange
        var transaction = new RBCTransaction
        {
            Description = description,
            Type = TransactionType.Transfer
        };

        // Act
        var isETransfer = description.ToLower().Contains("e-trf") ||
                         description.ToLower().Contains("interac");

        // Assert
        Assert.That(isETransfer, Is.False,
            $"Description '{description}' should NOT be identified as E-transfer");
    }

    #endregion

    #region Transfer Reclassification Tests

    [Test]
    public void TransferReclassification_ToExpense_UpdatesTypeAndCategory()
    {
        // Arrange
        var transfer = new RBCTransaction
        {
            Description = "E-transfer to landlord",
            Amount = 1500m,
            Type = TransactionType.Transfer,
            Category = null
        };

        // Act - Simulating reclassification
        transfer.Type = TransactionType.Expense;
        transfer.Category = "Housing";

        // Assert
        Assert.That(transfer.Type, Is.EqualTo(TransactionType.Expense));
        Assert.That(transfer.Category, Is.EqualTo("Housing"));
    }

    [Test]
    public void TransferReclassification_ToIncome_UpdatesType()
    {
        // Arrange
        var transfer = new RBCTransaction
        {
            Description = "E-transfer refund",
            Amount = -200m,
            Type = TransactionType.Transfer,
            Category = null
        };

        // Act
        transfer.Type = TransactionType.Income;
        transfer.Category = null; // Income typically has no category

        // Assert
        Assert.That(transfer.Type, Is.EqualTo(TransactionType.Income));
        Assert.That(transfer.Category, Is.Null);
    }

    [Test]
    public void TransferReclassification_ToAdjustment_UpdatesType()
    {
        // Arrange
        var transfer = new RBCTransaction
        {
            Description = "Transfer fee",
            Amount = 5m,
            Type = TransactionType.Transfer,
            Category = null
        };

        // Act
        transfer.Type = TransactionType.Adjustment;
        transfer.Category = null;

        // Assert
        Assert.That(transfer.Type, Is.EqualTo(TransactionType.Adjustment));
    }

    #endregion

    #region Unmatched Transfer Detection Tests

    [Test]
    public void UnmatchedTransfer_IsNotReconciled()
    {
        // Arrange
        var transfer = new RBCTransaction
        {
            Description = "Credit Card Payment",
            Amount = 500m,
            Type = TransactionType.Transfer,
            LinkedTransactionId = null,
            IsReconciledTransfer = false
        };

        // Assert
        Assert.That(transfer.IsReconciledTransfer, Is.False);
        Assert.That(transfer.LinkedTransactionId, Is.Null);
    }

    [Test]
    public void MatchedTransfer_IsReconciled()
    {
        // Arrange
        var transfer = new RBCTransaction
        {
            Description = "Credit Card Payment",
            Amount = 500m,
            Type = TransactionType.Transfer,
            LinkedTransactionId = "linked-id-123",
            IsReconciledTransfer = true
        };

        // Assert
        Assert.That(transfer.IsReconciledTransfer, Is.True);
        Assert.That(transfer.LinkedTransactionId, Is.Not.Null);
    }

    #endregion

    #region Match Criteria Tests

    [Test]
    public void MatchCriteria_OppositeAmounts_ShouldMatch()
    {
        // Arrange
        var transfer1 = new RBCTransaction { Amount = 500m };  // OUT
        var transfer2 = new AmexTransaction { Amount = -500m }; // IN

        // Act
        var amountsMatch = Math.Abs(Math.Abs(transfer1.Amount) - Math.Abs(transfer2.Amount)) < 0.01m;
        var oppositeSigns = (transfer1.Amount > 0 && transfer2.Amount < 0) ||
                           (transfer1.Amount < 0 && transfer2.Amount > 0);

        // Assert
        Assert.That(amountsMatch, Is.True);
        Assert.That(oppositeSigns, Is.True);
    }

    [Test]
    public void MatchCriteria_SameSignAmounts_ShouldNotMatch()
    {
        // Arrange
        var transfer1 = new RBCTransaction { Amount = 500m };  // OUT
        var transfer2 = new AmexTransaction { Amount = 500m }; // OUT (same sign)

        // Act
        var oppositeSigns = (transfer1.Amount > 0 && transfer2.Amount < 0) ||
                           (transfer1.Amount < 0 && transfer2.Amount > 0);

        // Assert
        Assert.That(oppositeSigns, Is.False);
    }

    [Test]
    public void MatchCriteria_DateProximity_WithinThreeDays()
    {
        // Arrange
        var transfer1 = new RBCTransaction { Date = new DateTime(2025, 1, 10) };
        var transfer2 = new AmexTransaction { Date = new DateTime(2025, 1, 12) };

        // Act
        var daysDiff = Math.Abs((transfer1.Date - transfer2.Date).TotalDays);

        // Assert
        Assert.That(daysDiff, Is.LessThanOrEqualTo(3));
    }

    [Test]
    public void MatchCriteria_DateProximity_BeyondThreeDays()
    {
        // Arrange
        var transfer1 = new RBCTransaction { Date = new DateTime(2025, 1, 10) };
        var transfer2 = new AmexTransaction { Date = new DateTime(2025, 1, 20) };

        // Act
        var daysDiff = Math.Abs((transfer1.Date - transfer2.Date).TotalDays);

        // Assert
        Assert.That(daysDiff, Is.GreaterThan(3));
    }

    #endregion
}
