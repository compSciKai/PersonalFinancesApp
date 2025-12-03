using NUnit.Framework;
using PersonalFinances.App;
using PersonalFinances.Models;

namespace PersonalFinancesAppTests;

[TestFixture]
public class TransactionTypeDetectorTests
{
    private TransactionTypeDetector _detector = null!;

    [SetUp]
    public void Setup()
    {
        _detector = new TransactionTypeDetector();
    }

    #region Vendor Override Tests

    [Test]
    public void DetectType_WithVendorOverride_ReturnsOverriddenType()
    {
        // Arrange
        var transaction = new RBCTransaction
        {
            Description = "Regular Purchase",
            Amount = 100m,
            Vendor = "Test Vendor"
        };

        var vendorMapping = new VendorMapping
        {
            VendorName = "Test Vendor",
            OverrideType = true,
            SuggestedType = TransactionType.Income
        };

        // Act
        var result = _detector.DetectType(transaction, vendorMapping, null);

        // Assert
        Assert.That(result, Is.EqualTo(TransactionType.Income));
    }

    [Test]
    public void DetectType_WithVendorSuggestionButNoOverride_UsesKeywordDetection()
    {
        // Arrange
        var transaction = new RBCTransaction
        {
            Description = "E-transfer payment",
            Amount = 100m,
            Vendor = "Test Vendor"
        };

        var vendorMapping = new VendorMapping
        {
            VendorName = "Test Vendor",
            OverrideType = false, // Suggestion only, not override
            SuggestedType = TransactionType.Income
        };

        // Act - keyword "transfer" should take precedence
        var result = _detector.DetectType(transaction, vendorMapping, null);

        // Assert
        Assert.That(result, Is.EqualTo(TransactionType.Transfer));
    }

    #endregion

    #region Amount-Based Detection Tests

    [Test]
    public void DetectType_WithNegativeAmount_ReturnsIncome()
    {
        // Arrange - For RBC (isNegativeAmounts=true), positive amounts = income
        var transaction = new RBCTransaction
        {
            Description = "Paycheck",
            Amount = 1500m, // Positive = money in for RBC
            Vendor = "Employer"
        };

        // Act
        var result = _detector.DetectType(transaction, null, null);

        // Assert
        Assert.That(result, Is.EqualTo(TransactionType.Income));
    }

    [Test]
    public void DetectType_WithPositiveAmount_ContinuesToKeywordDetection()
    {
        // Arrange
        var transaction = new RBCTransaction
        {
            Description = "Credit card payment transfer",
            Amount = 500m,
            Vendor = "Credit Card"
        };

        // Act
        var result = _detector.DetectType(transaction, null, null);

        // Assert - Should detect as Transfer due to keywords, not Income
        Assert.That(result, Is.EqualTo(TransactionType.Transfer));
    }

    #endregion

    #region Transfer Keyword Detection Tests

    [TestCase("E-transfer to John")]
    [TestCase("INTERAC E-TRF")]
    [TestCase("etransfer payment")]
    [TestCase("withdrawal from savings")]
    [TestCase("deposit to checking")]
    [TestCase("payment to account 1234")]
    [TestCase("TFSA contribution")]
    [TestCase("RRSP deposit")]
    [TestCase("loan payment")]
    [TestCase("mortgage payment")]
    [TestCase("credit payment")]
    public void DetectType_WithTransferKeywords_ReturnsTransfer(string description)
    {
        // Arrange
        var transaction = new RBCTransaction
        {
            Description = description,
            Amount = 100m,
            Vendor = "Bank"
        };

        // Act
        var result = _detector.DetectType(transaction, null, null);

        // Assert
        Assert.That(result, Is.EqualTo(TransactionType.Transfer),
            $"Expected Transfer for description: '{description}'");
    }

    [Test]
    public void DetectType_TransferKeywordInVendor_ReturnsTransfer()
    {
        // Arrange
        var transaction = new RBCTransaction
        {
            Description = "Monthly payment",
            Amount = 100m,
            Vendor = "account transfer"
        };

        // Act
        var result = _detector.DetectType(transaction, null, null);

        // Assert
        Assert.That(result, Is.EqualTo(TransactionType.Transfer));
    }

    [Test]
    public void DetectType_TransferKeywordInCategory_ReturnsTransfer()
    {
        // Arrange
        var transaction = new RBCTransaction
        {
            Description = "Monthly payment",
            Amount = 100m,
            Vendor = "Bank",
            Category = "loan payment"
        };

        var category = new Category
        {
            CategoryName = "loan payment"
        };

        // Act
        var result = _detector.DetectType(transaction, null, category);

        // Assert
        Assert.That(result, Is.EqualTo(TransactionType.Transfer));
    }

    #endregion

    #region Adjustment Keyword Detection Tests

    [TestCase("Service fee")]
    [TestCase("Monthly fee")]
    [TestCase("interest charge")]
    [TestCase("Overdraft fee")]
    [TestCase("cashback reward")]
    [TestCase("Cash reward")]
    [TestCase("refund for purchase")]
    [TestCase("Transaction reversal")]
    public void DetectType_WithAdjustmentKeywords_ReturnsAdjustment(string description)
    {
        // Arrange
        var transaction = new RBCTransaction
        {
            Description = description,
            Amount = 5m,
            Vendor = "Bank"
        };

        // Act
        var result = _detector.DetectType(transaction, null, null);

        // Assert
        Assert.That(result, Is.EqualTo(TransactionType.Adjustment),
            $"Expected Adjustment for description: '{description}'");
    }

    [Test]
    public void DetectType_AdjustmentKeywordInVendor_ReturnsAdjustment()
    {
        // Arrange
        var transaction = new RBCTransaction
        {
            Description = "Monthly charge",
            Amount = 5m,
            Vendor = "interest charge"
        };

        // Act
        var result = _detector.DetectType(transaction, null, null);

        // Assert
        Assert.That(result, Is.EqualTo(TransactionType.Adjustment));
    }

    #endregion

    #region Default Detection Tests

    [Test]
    public void DetectType_NoMatchingKeywords_ReturnsExpense()
    {
        // Arrange - For RBC (isNegativeAmounts=true), negative amounts = expenses
        var transaction = new RBCTransaction
        {
            Description = "Grocery Store Purchase",
            Amount = -50m, // Negative = money out = expense for RBC
            Vendor = "Walmart"
        };

        // Act
        var result = _detector.DetectType(transaction, null, null);

        // Assert
        Assert.That(result, Is.EqualTo(TransactionType.Expense));
    }

    [Test]
    public void DetectType_EmptyDescription_ReturnsExpense()
    {
        // Arrange - For RBC (isNegativeAmounts=true), negative amounts = expenses
        var transaction = new RBCTransaction
        {
            Description = "",
            Amount = -50m, // Negative = money out = expense for RBC
            Vendor = "Unknown"
        };

        // Act
        var result = _detector.DetectType(transaction, null, null);

        // Assert
        Assert.That(result, Is.EqualTo(TransactionType.Expense));
    }

    #endregion

    #region Priority Order Tests

    [Test]
    public void DetectType_VendorOverrideTakesPrecedenceOverAmount()
    {
        // Arrange - Negative amount would normally mean Income
        var transaction = new RBCTransaction
        {
            Description = "Special transaction",
            Amount = -1000m,
            Vendor = "Test Vendor"
        };

        var vendorMapping = new VendorMapping
        {
            VendorName = "Test Vendor",
            OverrideType = true,
            SuggestedType = TransactionType.Expense // Override says Expense
        };

        // Act
        var result = _detector.DetectType(transaction, vendorMapping, null);

        // Assert - Should use override, not amount-based detection
        Assert.That(result, Is.EqualTo(TransactionType.Expense));
    }

    [Test]
    public void DetectType_AmountTakesPrecedenceOverKeywords()
    {
        // Arrange - Description has "fee" (Adjustment keyword) and amount is positive (would be Income)
        // This tests that keywords take precedence over amount-based detection
        var transaction = new RBCTransaction
        {
            Description = "Account fee",
            Amount = 25m, // Positive = would be Income based on amount alone
            Vendor = "Bank"
        };

        // Act
        var result = _detector.DetectType(transaction, null, null);

        // Assert - Adjustment (from keyword) should take precedence over Income (from amount)
        Assert.That(result, Is.EqualTo(TransactionType.Adjustment));
    }

    [Test]
    public void DetectType_TransferTakesPrecedenceOverAdjustment()
    {
        // Arrange - Has both transfer and adjustment keywords
        var transaction = new RBCTransaction
        {
            Description = "E-transfer fee", // Has both "e-transfer" and "fee"
            Amount = 50m,
            Vendor = "Bank"
        };

        // Act
        var result = _detector.DetectType(transaction, null, null);

        // Assert - Transfer should take precedence (detected first in priority order)
        Assert.That(result, Is.EqualTo(TransactionType.Transfer));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void DetectType_CaseInsensitiveKeywordMatching()
    {
        // Arrange
        var transaction = new RBCTransaction
        {
            Description = "E-TRF INTERAC", // All caps
            Amount = 100m,
            Vendor = "BANK"
        };

        // Act
        var result = _detector.DetectType(transaction, null, null);

        // Assert
        Assert.That(result, Is.EqualTo(TransactionType.Transfer));
    }

    [Test]
    public void DetectType_PartialKeywordMatch()
    {
        // Arrange
        var transaction = new RBCTransaction
        {
            Description = "E-transfered funds", // Contains "e-transfer" as part of word
            Amount = 100m,
            Vendor = "Bank"
        };

        // Act
        var result = _detector.DetectType(transaction, null, null);

        // Assert
        Assert.That(result, Is.EqualTo(TransactionType.Transfer));
    }

    [Test]
    public void DetectType_NullVendorAndCategory_StillWorks()
    {
        // Arrange
        var transaction = new RBCTransaction
        {
            Description = "Fee charge",
            Amount = 5m,
            Vendor = null,
            Category = null
        };

        // Act
        var result = _detector.DetectType(transaction, null, null);

        // Assert - Should still detect from description
        Assert.That(result, Is.EqualTo(TransactionType.Adjustment));
    }

    #endregion
}
