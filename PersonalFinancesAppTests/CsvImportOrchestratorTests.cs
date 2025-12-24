using Moq;
using NUnit.Framework;
using PersonalFinances.App;
using PersonalFinances.Models;

namespace PersonalFinancesAppTests
{
    /// <summary>
    /// Tests for CsvImportOrchestrator covering priority logic, user interaction, and file discovery coordination.
    /// </summary>
    [TestFixture]
    public class CsvImportOrchestratorTests
    {
        private Mock<ICsvFileDiscoveryService> _csvFileDiscoveryServiceMock;
        private Mock<ITransactionsUserInteraction> _userInteractionMock;
        private CsvImportOrchestrator _orchestrator;

        [SetUp]
        public void Setup()
        {
            _csvFileDiscoveryServiceMock = new Mock<ICsvFileDiscoveryService>();
            _userInteractionMock = new Mock<ITransactionsUserInteraction>();
            _orchestrator = new CsvImportOrchestrator(_csvFileDiscoveryServiceMock.Object);
        }

        #region GetTransactionsToProcessAsync Tests

        [Test]
        public async Task GetTransactionsToProcessAsync_ReturnsHardcodedDictionary_WhenProvidedAndNotEmpty()
        {
            // Arrange - Hardcoded dictionary takes priority (backward compatibility)
            var hardcodedDictionary = new Dictionary<string, Type>
            {
                { "C:\\path\\to\\file1.csv", typeof(RBCTransaction) },
                { "C:\\path\\to\\file2.csv", typeof(AmexTransaction) }
            };

            var csvSettings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = "C:\\RBC\\Input" }
            };

            // Act
            var result = await _orchestrator.GetTransactionsToProcessAsync(
                hardcodedDictionary,
                csvSettings,
                _userInteractionMock.Object);

            // Assert
            Assert.IsNotNull(result, "Should return a dictionary");
            Assert.AreEqual(2, result.Count, "Should return the hardcoded dictionary unchanged");
            Assert.AreSame(hardcodedDictionary, result, "Should return the exact same dictionary instance");

            // Verify that folder discovery was NOT called (backward compatibility mode)
            _csvFileDiscoveryServiceMock.Verify(
                s => s.EnsureFoldersExistAsync(It.IsAny<CsvImportSettings>()),
                Times.Never,
                "Should not call EnsureFoldersExistAsync in backward compatibility mode");

            _csvFileDiscoveryServiceMock.Verify(
                s => s.DiscoverCsvFilesAsync(It.IsAny<CsvImportSettings>()),
                Times.Never,
                "Should not call DiscoverCsvFilesAsync in backward compatibility mode");
        }

        [Test]
        public async Task GetTransactionsToProcessAsync_ScansForFiles_WhenDictionaryIsNull()
        {
            // Arrange
            var csvSettings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = "C:\\RBC\\Input" }
            };

            var discoveredFiles = new Dictionary<string, Type>
            {
                { "C:\\RBC\\Input\\file1.csv", typeof(RBCTransaction) }
            };

            _csvFileDiscoveryServiceMock.Setup(s => s.DiscoverCsvFilesAsync(csvSettings))
                .ReturnsAsync(discoveredFiles);

            _userInteractionMock.Setup(ui => ui.GetInput())
                .Returns("y");

            // Act
            var result = await _orchestrator.GetTransactionsToProcessAsync(
                null,
                csvSettings,
                _userInteractionMock.Object);

            // Assert
            Assert.IsNotNull(result, "Should return a dictionary");
            Assert.AreEqual(1, result.Count, "Should return discovered files");

            // Verify that folder creation was called before scanning
            _csvFileDiscoveryServiceMock.Verify(
                s => s.EnsureFoldersExistAsync(csvSettings),
                Times.Once,
                "Should call EnsureFoldersExistAsync before scanning");

            _csvFileDiscoveryServiceMock.Verify(
                s => s.DiscoverCsvFilesAsync(csvSettings),
                Times.Once,
                "Should call DiscoverCsvFilesAsync");
        }

        [Test]
        public async Task GetTransactionsToProcessAsync_ScansForFiles_WhenDictionaryIsEmpty()
        {
            // Arrange
            var emptyDictionary = new Dictionary<string, Type>();
            var csvSettings = new CsvImportSettings
            {
                Amex = new CsvTransactionTypeSettings { InputFolder = "C:\\Amex\\Input" }
            };

            var discoveredFiles = new Dictionary<string, Type>
            {
                { "C:\\Amex\\Input\\file1.csv", typeof(AmexTransaction) }
            };

            _csvFileDiscoveryServiceMock.Setup(s => s.DiscoverCsvFilesAsync(csvSettings))
                .ReturnsAsync(discoveredFiles);

            _userInteractionMock.Setup(ui => ui.GetInput())
                .Returns("y");

            // Act
            var result = await _orchestrator.GetTransactionsToProcessAsync(
                emptyDictionary,
                csvSettings,
                _userInteractionMock.Object);

            // Assert
            Assert.IsNotNull(result, "Should return a dictionary");
            Assert.AreEqual(1, result.Count, "Should return discovered files");

            _csvFileDiscoveryServiceMock.Verify(
                s => s.EnsureFoldersExistAsync(csvSettings),
                Times.Once,
                "Should call EnsureFoldersExistAsync");
        }

        [Test]
        public async Task GetTransactionsToProcessAsync_ReturnsEmptyDictionary_WhenNoFilesDiscovered()
        {
            // Arrange
            var csvSettings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = "C:\\RBC\\Input" }
            };

            var emptyDiscoveredFiles = new Dictionary<string, Type>();

            _csvFileDiscoveryServiceMock.Setup(s => s.DiscoverCsvFilesAsync(csvSettings))
                .ReturnsAsync(emptyDiscoveredFiles);

            // Act
            var result = await _orchestrator.GetTransactionsToProcessAsync(
                null,
                csvSettings,
                _userInteractionMock.Object);

            // Assert
            Assert.IsNotNull(result, "Should return a dictionary");
            Assert.IsEmpty(result, "Should return empty dictionary when no files discovered");

            // User should NOT be prompted when no files are found
            _userInteractionMock.Verify(
                ui => ui.GetInput(),
                Times.Never,
                "Should not prompt user when no files are discovered");
        }

        [Test]
        public async Task GetTransactionsToProcessAsync_ReturnsDiscoveredFiles_WhenUserAnswersYes()
        {
            // Arrange
            var csvSettings = new CsvImportSettings
            {
                PCFinancial = new CsvTransactionTypeSettings { InputFolder = "C:\\PC\\Input" }
            };

            var discoveredFiles = new Dictionary<string, Type>
            {
                { "C:\\PC\\Input\\file1.csv", typeof(PCFinancialTransaction) },
                { "C:\\PC\\Input\\file2.csv", typeof(PCFinancialTransaction) }
            };

            _csvFileDiscoveryServiceMock.Setup(s => s.DiscoverCsvFilesAsync(csvSettings))
                .ReturnsAsync(discoveredFiles);

            _userInteractionMock.Setup(ui => ui.GetInput())
                .Returns("y");

            // Act
            var result = await _orchestrator.GetTransactionsToProcessAsync(
                null,
                csvSettings,
                _userInteractionMock.Object);

            // Assert
            Assert.IsNotNull(result, "Should return a dictionary");
            Assert.AreEqual(2, result.Count, "Should return discovered files");
            Assert.AreSame(discoveredFiles, result, "Should return the discovered files dictionary");

            _userInteractionMock.Verify(ui => ui.GetInput(), Times.Once, "Should prompt user once");
        }

        [Test]
        public async Task GetTransactionsToProcessAsync_ReturnsEmptyDictionary_WhenUserAnswersNo()
        {
            // Arrange
            var csvSettings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = "C:\\RBC\\Input" }
            };

            var discoveredFiles = new Dictionary<string, Type>
            {
                { "C:\\RBC\\Input\\file1.csv", typeof(RBCTransaction) }
            };

            _csvFileDiscoveryServiceMock.Setup(s => s.DiscoverCsvFilesAsync(csvSettings))
                .ReturnsAsync(discoveredFiles);

            _userInteractionMock.Setup(ui => ui.GetInput())
                .Returns("n");

            // Act
            var result = await _orchestrator.GetTransactionsToProcessAsync(
                null,
                csvSettings,
                _userInteractionMock.Object);

            // Assert
            Assert.IsNotNull(result, "Should return a dictionary");
            Assert.IsEmpty(result, "Should return empty dictionary when user answers 'n'");

            _userInteractionMock.Verify(ui => ui.GetInput(), Times.Once, "Should prompt user once");
        }

        [Test]
        public async Task GetTransactionsToProcessAsync_RePromptsUser_WhenInvalidInputProvided()
        {
            // Arrange
            var csvSettings = new CsvImportSettings
            {
                Amex = new CsvTransactionTypeSettings { InputFolder = "C:\\Amex\\Input" }
            };

            var discoveredFiles = new Dictionary<string, Type>
            {
                { "C:\\Amex\\Input\\file1.csv", typeof(AmexTransaction) }
            };

            _csvFileDiscoveryServiceMock.Setup(s => s.DiscoverCsvFilesAsync(csvSettings))
                .ReturnsAsync(discoveredFiles);

            // Setup sequence: invalid input, then another invalid, then valid "yes"
            var inputSequence = new Queue<string>(new[] { "invalid", "xyz", "yes" });
            _userInteractionMock.Setup(ui => ui.GetInput())
                .Returns(() => inputSequence.Dequeue());

            // Act
            var result = await _orchestrator.GetTransactionsToProcessAsync(
                null,
                csvSettings,
                _userInteractionMock.Object);

            // Assert
            Assert.IsNotNull(result, "Should return a dictionary");
            Assert.AreEqual(1, result.Count, "Should eventually return discovered files");

            _userInteractionMock.Verify(
                ui => ui.GetInput(),
                Times.Exactly(3),
                "Should prompt user 3 times (2 invalid + 1 valid)");
        }

        [Test]
        public async Task GetTransactionsToProcessAsync_AcceptsCaseInsensitiveInput_ForYesAndNo()
        {
            // Arrange
            var csvSettings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = "C:\\RBC\\Input" }
            };

            var discoveredFiles = new Dictionary<string, Type>
            {
                { "C:\\RBC\\Input\\file1.csv", typeof(RBCTransaction) }
            };

            _csvFileDiscoveryServiceMock.Setup(s => s.DiscoverCsvFilesAsync(csvSettings))
                .ReturnsAsync(discoveredFiles);

            // Test various case variations
            var testCases = new[] { "Y", "YES", "Yes", "yEs" };

            foreach (var testInput in testCases)
            {
                _userInteractionMock.Setup(ui => ui.GetInput()).Returns(testInput);

                // Act
                var result = await _orchestrator.GetTransactionsToProcessAsync(
                    null,
                    csvSettings,
                    _userInteractionMock.Object);

                // Assert
                Assert.AreEqual(1, result.Count, $"Should accept '{testInput}' as valid 'yes' input");
            }

            // Test 'no' variations
            var noCases = new[] { "N", "NO", "No", "nO" };
            foreach (var testInput in noCases)
            {
                _userInteractionMock.Setup(ui => ui.GetInput()).Returns(testInput);

                // Act
                var result = await _orchestrator.GetTransactionsToProcessAsync(
                    null,
                    csvSettings,
                    _userInteractionMock.Object);

                // Assert
                Assert.IsEmpty(result, $"Should accept '{testInput}' as valid 'no' input");
            }
        }

        [Test]
        public async Task GetTransactionsToProcessAsync_EnsuresFoldersExistBeforeDiscovery()
        {
            // Arrange
            var csvSettings = new CsvImportSettings
            {
                RBC = new CsvTransactionTypeSettings { InputFolder = "C:\\RBC\\Input" }
            };

            var discoveredFiles = new Dictionary<string, Type>();
            _csvFileDiscoveryServiceMock.Setup(s => s.DiscoverCsvFilesAsync(csvSettings))
                .ReturnsAsync(discoveredFiles);

            var callOrder = 0;
            var ensureFoldersCallOrder = 0;
            var discoverFilesCallOrder = 0;

            _csvFileDiscoveryServiceMock.Setup(s => s.EnsureFoldersExistAsync(csvSettings))
                .Callback(() => ensureFoldersCallOrder = ++callOrder)
                .Returns(Task.CompletedTask);

            _csvFileDiscoveryServiceMock.Setup(s => s.DiscoverCsvFilesAsync(csvSettings))
                .Callback(() => discoverFilesCallOrder = ++callOrder)
                .ReturnsAsync(discoveredFiles);

            // Act
            await _orchestrator.GetTransactionsToProcessAsync(
                null,
                csvSettings,
                _userInteractionMock.Object);

            // Assert - Verify EnsureFoldersExistAsync was called before DiscoverCsvFilesAsync
            Assert.That(ensureFoldersCallOrder, Is.LessThan(discoverFilesCallOrder),
                "EnsureFoldersExistAsync should be called before DiscoverCsvFilesAsync");

            _csvFileDiscoveryServiceMock.Verify(
                s => s.EnsureFoldersExistAsync(csvSettings),
                Times.Once,
                "Should call EnsureFoldersExistAsync");
        }

        #endregion
    }
}
