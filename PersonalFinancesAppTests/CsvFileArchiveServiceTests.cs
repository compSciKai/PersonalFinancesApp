using NUnit.Framework;
using PersonalFinances.App;
using PersonalFinances.Models;

namespace PersonalFinancesAppTests
{
    /// <summary>
    /// Tests for CsvFileArchiveService covering file archiving, date extraction, and error handling.
    /// </summary>
    [TestFixture]
    public class CsvFileArchiveServiceTests
    {
        private CsvFileArchiveService _service;
        private string _testRootDirectory;

        [SetUp]
        public void Setup()
        {
            _service = new CsvFileArchiveService();

            // Create unique test directory for each test run
            _testRootDirectory = Path.Combine(Path.GetTempPath(), $"CsvArchiveTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testRootDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup test directory after each test
            if (Directory.Exists(_testRootDirectory))
            {
                try
                {
                    Directory.Delete(_testRootDirectory, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        #region ArchiveFileAsync Tests

        [Test]
        public async Task ArchiveFileAsync_MovesFileWithCorrectDateSuffix_WhenTransactionsProvided()
        {
            // Arrange
            var inputFolder = Path.Combine(_testRootDirectory, "Input");
            var processedFolder = Path.Combine(_testRootDirectory, "Processed");
            Directory.CreateDirectory(inputFolder);
            Directory.CreateDirectory(processedFolder);

            var sourceFile = Path.Combine(inputFolder, "transactions.csv");
            File.WriteAllText(sourceFile, "test data");

            var transactions = new List<Transaction>
            {
                new RBCTransaction { Date = new DateTime(2024, 1, 15), Amount = 100, Description = "Test 1" },
                new RBCTransaction { Date = new DateTime(2024, 2, 20), Amount = 200, Description = "Test 2" },
                new RBCTransaction { Date = new DateTime(2024, 1, 10), Amount = 150, Description = "Test 3" }
            };

            // Act
            var result = await _service.ArchiveFileAsync(sourceFile, processedFolder, transactions);

            // Assert
            Assert.IsTrue(result, "Archive operation should succeed");
            Assert.IsFalse(File.Exists(sourceFile), "Source file should be moved");

            var expectedFileName = "rbc_20240220.csv"; // RBC type with latest date 2024-02-20
            var expectedDestination = Path.Combine(processedFolder, expectedFileName);
            Assert.IsTrue(File.Exists(expectedDestination), $"Archived file should exist at {expectedDestination}");
        }

        [Test]
        public async Task ArchiveFileAsync_UsesTodaysDate_WhenTransactionListIsEmpty()
        {
            // Arrange
            var inputFolder = Path.Combine(_testRootDirectory, "Input");
            var processedFolder = Path.Combine(_testRootDirectory, "Processed");
            Directory.CreateDirectory(inputFolder);
            Directory.CreateDirectory(processedFolder);

            var sourceFile = Path.Combine(inputFolder, "empty_transactions.csv");
            File.WriteAllText(sourceFile, "test data");

            var emptyTransactions = new List<Transaction>();

            // Act
            var result = await _service.ArchiveFileAsync(sourceFile, processedFolder, emptyTransactions);

            // Assert
            Assert.IsTrue(result, "Archive operation should succeed");
            Assert.IsFalse(File.Exists(sourceFile), "Source file should be moved");

            var todayDateString = DateTime.Today.ToString("yyyyMMdd");
            var expectedFileName = $"unknown_{todayDateString}.csv"; // Empty list = unknown type
            var expectedDestination = Path.Combine(processedFolder, expectedFileName);
            Assert.IsTrue(File.Exists(expectedDestination), "Archived file should use today's date");
        }

        [Test]
        public async Task ArchiveFileAsync_CreatesProcessedFolder_WhenItDoesNotExist()
        {
            // Arrange
            var inputFolder = Path.Combine(_testRootDirectory, "Input");
            var processedFolder = Path.Combine(_testRootDirectory, "NonExistentProcessed");
            Directory.CreateDirectory(inputFolder);

            var sourceFile = Path.Combine(inputFolder, "transactions.csv");
            File.WriteAllText(sourceFile, "test data");

            var transactions = new List<Transaction>
            {
                new RBCTransaction { Date = new DateTime(2024, 3, 15), Amount = 100, Description = "Test" }
            };

            // Act
            var result = await _service.ArchiveFileAsync(sourceFile, processedFolder, transactions);

            // Assert
            Assert.IsTrue(result, "Archive operation should succeed");
            Assert.IsTrue(Directory.Exists(processedFolder), "Processed folder should be created");
            Assert.IsFalse(File.Exists(sourceFile), "Source file should be moved");
        }

        [Test]
        public async Task ArchiveFileAsync_ReturnsFalse_WhenDestinationFileAlreadyExists()
        {
            // Arrange
            var inputFolder = Path.Combine(_testRootDirectory, "Input");
            var processedFolder = Path.Combine(_testRootDirectory, "Processed");
            Directory.CreateDirectory(inputFolder);
            Directory.CreateDirectory(processedFolder);

            var sourceFile = Path.Combine(inputFolder, "transactions.csv");
            File.WriteAllText(sourceFile, "test data");

            var transactions = new List<Transaction>
            {
                new RBCTransaction { Date = new DateTime(2024, 5, 10), Amount = 100, Description = "Test" }
            };

            // Pre-create the destination file with new naming format
            var expectedFileName = "rbc_20240510.csv";
            var destinationFile = Path.Combine(processedFolder, expectedFileName);
            File.WriteAllText(destinationFile, "existing file");

            // Act
            var result = await _service.ArchiveFileAsync(sourceFile, processedFolder, transactions);

            // Assert
            Assert.IsFalse(result, "Archive should fail when destination file already exists");
            Assert.IsTrue(File.Exists(sourceFile), "Source file should remain (not overwritten)");
            Assert.AreEqual("existing file", File.ReadAllText(destinationFile), "Existing file should not be overwritten");
        }

        [Test]
        public async Task ArchiveFileAsync_ReturnsFalse_WhenSourcePathIsNull()
        {
            // Arrange
            var processedFolder = Path.Combine(_testRootDirectory, "Processed");
            var transactions = new List<Transaction>();

            // Act
            var result = await _service.ArchiveFileAsync(null, processedFolder, transactions);

            // Assert
            Assert.IsFalse(result, "Should return false when source path is null");
        }

        [Test]
        public async Task ArchiveFileAsync_ReturnsFalse_WhenSourcePathIsEmpty()
        {
            // Arrange
            var processedFolder = Path.Combine(_testRootDirectory, "Processed");
            var transactions = new List<Transaction>();

            // Act
            var result = await _service.ArchiveFileAsync(string.Empty, processedFolder, transactions);

            // Assert
            Assert.IsFalse(result, "Should return false when source path is empty");
        }

        [Test]
        public async Task ArchiveFileAsync_ReturnsFalse_WhenProcessedFolderIsNull()
        {
            // Arrange
            var inputFolder = Path.Combine(_testRootDirectory, "Input");
            Directory.CreateDirectory(inputFolder);

            var sourceFile = Path.Combine(inputFolder, "transactions.csv");
            File.WriteAllText(sourceFile, "test data");

            var transactions = new List<Transaction>();

            // Act
            var result = await _service.ArchiveFileAsync(sourceFile, null, transactions);

            // Assert
            Assert.IsFalse(result, "Should return false when processed folder is null");
            Assert.IsTrue(File.Exists(sourceFile), "Source file should remain untouched");
        }

        [Test]
        public async Task ArchiveFileAsync_ReturnsFalse_WhenSourceFileDoesNotExist()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_testRootDirectory, "nonexistent.csv");
            var processedFolder = Path.Combine(_testRootDirectory, "Processed");
            var transactions = new List<Transaction>();

            // Act
            var result = await _service.ArchiveFileAsync(nonExistentFile, processedFolder, transactions);

            // Assert
            Assert.IsFalse(result, "Should return false when source file doesn't exist");
        }

        [Test]
        public async Task ArchiveFileAsync_UsesAmexName_WhenTransactionsAreAmexType()
        {
            // Arrange
            var inputFolder = Path.Combine(_testRootDirectory, "Input");
            var processedFolder = Path.Combine(_testRootDirectory, "Processed");
            Directory.CreateDirectory(inputFolder);
            Directory.CreateDirectory(processedFolder);

            var sourceFile = Path.Combine(inputFolder, "ugly-amex-name-from-bank.csv");
            File.WriteAllText(sourceFile, "test data");

            var transactions = new List<Transaction>
            {
                new AmexTransaction { Date = new DateTime(2024, 6, 15), Amount = 100, Description = "Test" }
            };

            // Act
            var result = await _service.ArchiveFileAsync(sourceFile, processedFolder, transactions);

            // Assert
            Assert.IsTrue(result, "Archive operation should succeed");
            var expectedFileName = "amex_20240615.csv";
            var expectedDestination = Path.Combine(processedFolder, expectedFileName);
            Assert.IsTrue(File.Exists(expectedDestination), "Archived file should have clean 'amex' name");
        }

        [Test]
        public async Task ArchiveFileAsync_UsesPCFinancialName_WhenTransactionsArePCFinancialType()
        {
            // Arrange
            var inputFolder = Path.Combine(_testRootDirectory, "Input");
            var processedFolder = Path.Combine(_testRootDirectory, "Processed");
            Directory.CreateDirectory(inputFolder);
            Directory.CreateDirectory(processedFolder);

            var sourceFile = Path.Combine(inputFolder, "messy-pc-financial-export-2024.csv");
            File.WriteAllText(sourceFile, "test data");

            var transactions = new List<Transaction>
            {
                new PCFinancialTransaction { Date = new DateTime(2024, 7, 20), Amount = 100, Description = "Test" }
            };

            // Act
            var result = await _service.ArchiveFileAsync(sourceFile, processedFolder, transactions);

            // Assert
            Assert.IsTrue(result, "Archive operation should succeed");
            var expectedFileName = "pcfinancial_20240720.csv";
            var expectedDestination = Path.Combine(processedFolder, expectedFileName);
            Assert.IsTrue(File.Exists(expectedDestination), "Archived file should have clean 'pcfinancial' name");
        }

        #endregion

        #region GetLatestTransactionDate Tests

        [Test]
        public void GetLatestTransactionDate_ReturnsMaxDate_WithRBCTransactions()
        {
            // Arrange
            var transactions = new List<Transaction>
            {
                new RBCTransaction { Date = new DateTime(2024, 1, 15), Amount = 100, Description = "Test 1" },
                new RBCTransaction { Date = new DateTime(2024, 3, 20), Amount = 200, Description = "Test 2" },
                new RBCTransaction { Date = new DateTime(2024, 2, 10), Amount = 150, Description = "Test 3" }
            };

            // Act
            var result = _service.GetLatestTransactionDate(transactions);

            // Assert
            Assert.AreEqual(new DateTime(2024, 3, 20), result, "Should return the latest date (2024-03-20)");
        }

        [Test]
        public void GetLatestTransactionDate_ReturnsMaxDate_WithAmexTransactions()
        {
            // Arrange
            var transactions = new List<Transaction>
            {
                new AmexTransaction { Date = new DateTime(2024, 6, 5), Amount = 50, Description = "Amex 1" },
                new AmexTransaction { Date = new DateTime(2024, 6, 15), Amount = 75, Description = "Amex 2" },
                new AmexTransaction { Date = new DateTime(2024, 5, 25), Amount = 100, Description = "Amex 3" }
            };

            // Act
            var result = _service.GetLatestTransactionDate(transactions);

            // Assert
            Assert.AreEqual(new DateTime(2024, 6, 15), result, "Should return the latest date (2024-06-15)");
        }

        [Test]
        public void GetLatestTransactionDate_ReturnsMaxDate_WithPCFinancialTransactions()
        {
            // Arrange
            var transactions = new List<Transaction>
            {
                new PCFinancialTransaction { Date = new DateTime(2024, 7, 1), Amount = 200, Description = "PC 1" },
                new PCFinancialTransaction { Date = new DateTime(2024, 7, 10), Amount = 150, Description = "PC 2" },
                new PCFinancialTransaction { Date = new DateTime(2024, 8, 5), Amount = 300, Description = "PC 3" }
            };

            // Act
            var result = _service.GetLatestTransactionDate(transactions);

            // Assert
            Assert.AreEqual(new DateTime(2024, 8, 5), result, "Should return the latest date (2024-08-05)");
        }

        [Test]
        public void GetLatestTransactionDate_ReturnsMaxDate_WithMixedTransactionTypes()
        {
            // Arrange - mix of RBC, Amex, and PC Financial transactions
            var transactions = new List<Transaction>
            {
                new RBCTransaction { Date = new DateTime(2024, 1, 15), Amount = 100, Description = "RBC" },
                new AmexTransaction { Date = new DateTime(2024, 2, 20), Amount = 200, Description = "Amex" },
                new PCFinancialTransaction { Date = new DateTime(2024, 3, 10), Amount = 150, Description = "PC" },
                new RBCTransaction { Date = new DateTime(2024, 2, 5), Amount = 50, Description = "RBC 2" }
            };

            // Act
            var result = _service.GetLatestTransactionDate(transactions);

            // Assert
            Assert.AreEqual(new DateTime(2024, 3, 10), result, "Should return the latest date across all types (2024-03-10)");
        }

        [Test]
        public void GetLatestTransactionDate_ReturnsTodaysDate_WhenListIsNull()
        {
            // Act
            var result = _service.GetLatestTransactionDate(null);

            // Assert
            Assert.AreEqual(DateTime.Today, result, "Should return today's date when list is null");
        }

        [Test]
        public void GetLatestTransactionDate_ReturnsTodaysDate_WhenListIsEmpty()
        {
            // Arrange
            var emptyTransactions = new List<Transaction>();

            // Act
            var result = _service.GetLatestTransactionDate(emptyTransactions);

            // Assert
            Assert.AreEqual(DateTime.Today, result, "Should return today's date when list is empty");
        }

        [Test]
        public void GetLatestTransactionDate_ReturnsSingleDate_WhenOnlyOneTransaction()
        {
            // Arrange
            var transactions = new List<Transaction>
            {
                new RBCTransaction { Date = new DateTime(2024, 9, 25), Amount = 100, Description = "Single" }
            };

            // Act
            var result = _service.GetLatestTransactionDate(transactions);

            // Assert
            Assert.AreEqual(new DateTime(2024, 9, 25), result, "Should return the single transaction's date");
        }

        #endregion
    }
}
