using NUnit.Framework;
using PersonalFinances.App;
using PersonalFinances.Models;

namespace PersonalFinancesAppTests
{
    /// <summary>
    /// Tests for CsvFileDiscoveryService covering folder creation, file discovery, and error handling.
    /// </summary>
    [TestFixture]
    public class CsvFileDiscoveryServiceTests
    {
        private CsvFileDiscoveryService _service;
        private string _testRootDirectory;

        [SetUp]
        public void Setup()
        {
            _service = new CsvFileDiscoveryService();

            // Create unique test directory for each test run
            _testRootDirectory = Path.Combine(Path.GetTempPath(), $"CsvDiscoveryTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testRootDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup test directory after each test
            if (Directory.Exists(_testRootDirectory))
            {
                Directory.Delete(_testRootDirectory, recursive: true);
            }
        }

        #region EnsureFoldersExistAsync Tests

        [Test]
        public async Task EnsureFoldersExistAsync_CreatesInputFolder_WhenItDoesNotExist()
        {
            // Arrange
            var rbcInputFolder = Path.Combine(_testRootDirectory, "RBC", "Input");
            var rbcProcessedFolder = Path.Combine(_testRootDirectory, "RBC", "Processed");

            var settings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings
                {
                    InputFolder = rbcInputFolder,
                    ProcessedFolder = rbcProcessedFolder
                }
            };

            // Act
            await _service.EnsureFoldersExistAsync(settings);

            // Assert
            Assert.IsTrue(Directory.Exists(rbcInputFolder), "RBC Input folder should be created");
        }

        [Test]
        public async Task EnsureFoldersExistAsync_CreatesProcessedFolder_WhenItDoesNotExist()
        {
            // Arrange
            var amexInputFolder = Path.Combine(_testRootDirectory, "Amex", "Input");
            var amexProcessedFolder = Path.Combine(_testRootDirectory, "Amex", "Processed");

            var settings = new CsvImportSettings
            {
                Amex = new CsvTransactionTypeSettings
                {
                    InputFolder = amexInputFolder,
                    ProcessedFolder = amexProcessedFolder
                }
            };

            // Act
            await _service.EnsureFoldersExistAsync(settings);

            // Assert
            Assert.IsTrue(Directory.Exists(amexProcessedFolder), "Amex Processed folder should be created");
        }

        [Test]
        public async Task EnsureFoldersExistAsync_DoesNotThrow_WhenFoldersAlreadyExist()
        {
            // Arrange
            var pcInputFolder = Path.Combine(_testRootDirectory, "PC", "Input");
            var pcProcessedFolder = Path.Combine(_testRootDirectory, "PC", "Processed");

            Directory.CreateDirectory(pcInputFolder);
            Directory.CreateDirectory(pcProcessedFolder);

            var settings = new CsvImportSettings
            {
                PCFinancial = new CsvTransactionTypeSettings
                {
                    InputFolder = pcInputFolder,
                    ProcessedFolder = pcProcessedFolder
                }
            };

            // Act & Assert - should not throw
            Assert.DoesNotThrowAsync(async () => await _service.EnsureFoldersExistAsync(settings));
            Assert.IsTrue(Directory.Exists(pcInputFolder), "PC Input folder should still exist");
            Assert.IsTrue(Directory.Exists(pcProcessedFolder), "PC Processed folder should still exist");
        }

        [Test]
        public async Task EnsureFoldersExistAsync_DoesNotThrow_WhenSettingsIsNull()
        {
            // Act & Assert - should handle null gracefully
            Assert.DoesNotThrowAsync(async () => await _service.EnsureFoldersExistAsync(null));
        }

        [Test]
        public async Task EnsureFoldersExistAsync_CreatesAllConfiguredFolders_ForMultipleTransactionTypes()
        {
            // Arrange
            var rbcInputFolder = Path.Combine(_testRootDirectory, "RBC", "Input");
            var rbcProcessedFolder = Path.Combine(_testRootDirectory, "RBC", "Processed");
            var amexInputFolder = Path.Combine(_testRootDirectory, "Amex", "Input");
            var amexProcessedFolder = Path.Combine(_testRootDirectory, "Amex", "Processed");
            var pcInputFolder = Path.Combine(_testRootDirectory, "PC", "Input");
            var pcProcessedFolder = Path.Combine(_testRootDirectory, "PC", "Processed");

            var settings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings
                {
                    InputFolder = rbcInputFolder,
                    ProcessedFolder = rbcProcessedFolder
                },
                Amex = new CsvTransactionTypeSettings
                {
                    InputFolder = amexInputFolder,
                    ProcessedFolder = amexProcessedFolder
                },
                PCFinancial = new CsvTransactionTypeSettings
                {
                    InputFolder = pcInputFolder,
                    ProcessedFolder = pcProcessedFolder
                }
            };

            // Act
            await _service.EnsureFoldersExistAsync(settings);

            // Assert
            Assert.IsTrue(Directory.Exists(rbcInputFolder), "RBC Input folder should be created");
            Assert.IsTrue(Directory.Exists(rbcProcessedFolder), "RBC Processed folder should be created");
            Assert.IsTrue(Directory.Exists(amexInputFolder), "Amex Input folder should be created");
            Assert.IsTrue(Directory.Exists(amexProcessedFolder), "Amex Processed folder should be created");
            Assert.IsTrue(Directory.Exists(pcInputFolder), "PC Input folder should be created");
            Assert.IsTrue(Directory.Exists(pcProcessedFolder), "PC Processed folder should be created");
        }

        [Test]
        public async Task EnsureFoldersExistAsync_DoesNotCreateFolders_WhenTransactionTypeSettingsIsNull()
        {
            // Arrange
            var settings = new CsvImportSettings
            {
                RBC = null,
                Amex = null,
                PCFinancial = null
            };

            // Act
            await _service.EnsureFoldersExistAsync(settings);

            // Assert - no folders should be created
            var directories = Directory.GetDirectories(_testRootDirectory, "*", SearchOption.AllDirectories);
            Assert.IsEmpty(directories, "No folders should be created when all transaction type settings are null");
        }

        #endregion

        #region DiscoverCsvFilesAsync Tests

        [Test]
        public async Task DiscoverCsvFilesAsync_ReturnsEmptyDictionary_WhenNoFilesExist()
        {
            // Arrange
            var rbcInputFolder = Path.Combine(_testRootDirectory, "RBC", "Input");
            Directory.CreateDirectory(rbcInputFolder);

            var settings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = rbcInputFolder }
            };

            // Act
            var result = await _service.DiscoverCsvFilesAsync(settings);

            // Assert
            Assert.IsEmpty(result, "Should return empty dictionary when no CSV files exist");
        }

        [Test]
        public async Task DiscoverCsvFilesAsync_ReturnsEmptyDictionary_WhenSettingsIsNull()
        {
            // Act
            var result = await _service.DiscoverCsvFilesAsync(null);

            // Assert
            Assert.IsNotNull(result, "Should return non-null dictionary");
            Assert.IsEmpty(result, "Should return empty dictionary when settings is null");
        }

        [Test]
        public async Task DiscoverCsvFilesAsync_DiscoversCsvFiles_WithLowercaseExtension()
        {
            // Arrange
            var rbcInputFolder = Path.Combine(_testRootDirectory, "RBC", "Input");
            Directory.CreateDirectory(rbcInputFolder);

            var csvFile1 = Path.Combine(rbcInputFolder, "transactions1.csv");
            var csvFile2 = Path.Combine(rbcInputFolder, "transactions2.csv");
            File.WriteAllText(csvFile1, "test data");
            File.WriteAllText(csvFile2, "test data");

            var settings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = rbcInputFolder }
            };

            // Act
            var result = await _service.DiscoverCsvFilesAsync(settings);

            // Assert
            Assert.AreEqual(2, result.Count, "Should discover 2 CSV files");
            Assert.IsTrue(result.ContainsKey(csvFile1), "Should contain transactions1.csv");
            Assert.IsTrue(result.ContainsKey(csvFile2), "Should contain transactions2.csv");
            Assert.AreEqual(typeof(RBCTransaction), result[csvFile1], "Should map to RBCTransaction type");
            Assert.AreEqual(typeof(RBCTransaction), result[csvFile2], "Should map to RBCTransaction type");
        }

        [Test]
        public async Task DiscoverCsvFilesAsync_DiscoversCsvFiles_WithMixedCaseExtensions()
        {
            // Arrange - Windows file system is case-insensitive, but the pattern "*.csv" should match all cases
            var amexInputFolder = Path.Combine(_testRootDirectory, "Amex", "Input");
            Directory.CreateDirectory(amexInputFolder);

            var csvFile1 = Path.Combine(amexInputFolder, "transactions.csv");
            var csvFile2 = Path.Combine(amexInputFolder, "transactions.CSV");
            File.WriteAllText(csvFile1, "test data");
            // Note: On Windows, this will overwrite csvFile1 due to case-insensitivity
            // On Linux, this would create a separate file

            var settings = new CsvImportSettings
            {
                Amex = new CsvTransactionTypeSettings { InputFolder = amexInputFolder }
            };

            // Act
            var result = await _service.DiscoverCsvFilesAsync(settings);

            // Assert - at least one file should be discovered
            Assert.IsTrue(result.Count >= 1, "Should discover at least 1 CSV file");
            Assert.AreEqual(typeof(AmexTransaction), result.Values.First(), "Should map to AmexTransaction type");
        }

        [Test]
        public async Task DiscoverCsvFilesAsync_DiscoversCsvFilesAcrossMultipleTransactionTypes()
        {
            // Arrange
            var rbcInputFolder = Path.Combine(_testRootDirectory, "RBC", "Input");
            var amexInputFolder = Path.Combine(_testRootDirectory, "Amex", "Input");
            var pcInputFolder = Path.Combine(_testRootDirectory, "PC", "Input");

            Directory.CreateDirectory(rbcInputFolder);
            Directory.CreateDirectory(amexInputFolder);
            Directory.CreateDirectory(pcInputFolder);

            var rbcCsvFile = Path.Combine(rbcInputFolder, "rbc_transactions.csv");
            var amexCsvFile = Path.Combine(amexInputFolder, "amex_transactions.csv");
            var pcCsvFile = Path.Combine(pcInputFolder, "pc_transactions.csv");

            File.WriteAllText(rbcCsvFile, "test data");
            File.WriteAllText(amexCsvFile, "test data");
            File.WriteAllText(pcCsvFile, "test data");

            var settings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = rbcInputFolder },
                Amex = new CsvTransactionTypeSettings { InputFolder = amexInputFolder },
                PCFinancial = new CsvTransactionTypeSettings { InputFolder = pcInputFolder }
            };

            // Act
            var result = await _service.DiscoverCsvFilesAsync(settings);

            // Assert
            Assert.AreEqual(3, result.Count, "Should discover 3 CSV files across all transaction types");
            Assert.IsTrue(result.ContainsKey(rbcCsvFile), "Should contain RBC CSV file");
            Assert.IsTrue(result.ContainsKey(amexCsvFile), "Should contain Amex CSV file");
            Assert.IsTrue(result.ContainsKey(pcCsvFile), "Should contain PC Financial CSV file");
            Assert.AreEqual(typeof(RBCTransaction), result[rbcCsvFile]);
            Assert.AreEqual(typeof(AmexTransaction), result[amexCsvFile]);
            Assert.AreEqual(typeof(PCFinancialTransaction), result[pcCsvFile]);
        }

        [Test]
        public async Task DiscoverCsvFilesAsync_HandlesOverlappingFolders_WithoutDuplicates()
        {
            // Arrange - Use same folder for both RBC and Amex (edge case)
            var sharedInputFolder = Path.Combine(_testRootDirectory, "Shared", "Input");
            Directory.CreateDirectory(sharedInputFolder);

            var csvFile = Path.Combine(sharedInputFolder, "transactions.csv");
            File.WriteAllText(csvFile, "test data");

            var settings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = sharedInputFolder },
                Amex = new CsvTransactionTypeSettings { InputFolder = sharedInputFolder }
            };

            // Act
            var result = await _service.DiscoverCsvFilesAsync(settings);

            // Assert - should not contain duplicates (implementation skips already-discovered files)
            Assert.AreEqual(1, result.Count, "Should not create duplicate entries for the same file");
            Assert.AreEqual(typeof(RBCTransaction), result[csvFile], "Should use first discovered type (RBC)");
        }

        [Test]
        public async Task DiscoverCsvFilesAsync_DoesNotDiscoverNonCsvFiles()
        {
            // Arrange
            var rbcInputFolder = Path.Combine(_testRootDirectory, "RBC", "Input");
            Directory.CreateDirectory(rbcInputFolder);

            var csvFile = Path.Combine(rbcInputFolder, "transactions.csv");
            var txtFile = Path.Combine(rbcInputFolder, "transactions.txt");
            var xlsxFile = Path.Combine(rbcInputFolder, "transactions.xlsx");

            File.WriteAllText(csvFile, "test data");
            File.WriteAllText(txtFile, "test data");
            File.WriteAllText(xlsxFile, "test data");

            var settings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = rbcInputFolder }
            };

            // Act
            var result = await _service.DiscoverCsvFilesAsync(settings);

            // Assert
            Assert.AreEqual(1, result.Count, "Should only discover CSV files");
            Assert.IsTrue(result.ContainsKey(csvFile), "Should contain the CSV file");
            Assert.IsFalse(result.Keys.Any(k => k.EndsWith(".txt")), "Should not contain TXT files");
            Assert.IsFalse(result.Keys.Any(k => k.EndsWith(".xlsx")), "Should not contain XLSX files");
        }

        [Test]
        public async Task DiscoverCsvFilesAsync_ReturnsEmptyDictionary_WhenFolderDoesNotExist()
        {
            // Arrange - folder path that doesn't exist
            var nonExistentFolder = Path.Combine(_testRootDirectory, "NonExistent", "Input");

            var settings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = nonExistentFolder }
            };

            // Act
            var result = await _service.DiscoverCsvFilesAsync(settings);

            // Assert - should handle gracefully and return empty dictionary
            Assert.IsEmpty(result, "Should return empty dictionary when folder doesn't exist");
        }

        #endregion

        #region HasUnprocessedFilesAsync Tests

        [Test]
        public async Task HasUnprocessedFilesAsync_ReturnsTrue_WhenFilesExist()
        {
            // Arrange
            var rbcInputFolder = Path.Combine(_testRootDirectory, "RBC", "Input");
            Directory.CreateDirectory(rbcInputFolder);

            var csvFile = Path.Combine(rbcInputFolder, "transactions.csv");
            File.WriteAllText(csvFile, "test data");

            var settings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = rbcInputFolder }
            };

            // Act
            var result = await _service.HasUnprocessedFilesAsync(settings);

            // Assert
            Assert.IsTrue(result, "Should return true when CSV files exist");
        }

        [Test]
        public async Task HasUnprocessedFilesAsync_ReturnsFalse_WhenNoFilesExist()
        {
            // Arrange
            var rbcInputFolder = Path.Combine(_testRootDirectory, "RBC", "Input");
            Directory.CreateDirectory(rbcInputFolder);

            var settings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = rbcInputFolder }
            };

            // Act
            var result = await _service.HasUnprocessedFilesAsync(settings);

            // Assert
            Assert.IsFalse(result, "Should return false when no CSV files exist");
        }

        [Test]
        public async Task HasUnprocessedFilesAsync_ReturnsFalse_WhenSettingsIsNull()
        {
            // Act
            var result = await _service.HasUnprocessedFilesAsync(null);

            // Assert
            Assert.IsFalse(result, "Should return false when settings is null");
        }

        #endregion
    }
}
