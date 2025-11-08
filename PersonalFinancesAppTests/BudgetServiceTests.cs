using Moq;
using PersonalFinances.Repositories;
using PersonalFinances.Models;
using PersonalFinances.App;

namespace PersonalFinancesAppTests
{
    public class BudgetServiceTests
    {
        private Mock<IBudgetRepository> _budgetRepositoryMock;
        private Mock<ITransactionsUserInteraction> _transactionUserInteractionMock;
        private BudgetService _cut;

        [SetUp]
        public void Setup()
        {
            _budgetRepositoryMock = new Mock<IBudgetRepository>();
            _transactionUserInteractionMock = new Mock<ITransactionsUserInteraction>();
        }

        public void SetupOneProfileExists()
        {
            var testProfile = new BudgetProfile(
                name: "TestProfile",
                budgetCategories: new Dictionary<string, double>(),
                income: 1000,
                username: "UserA",
                description: "Mock budget profile"
            );

            _budgetRepositoryMock.Setup(repo => repo.LoadBudgetProfilesAsync())
                .ReturnsAsync(new List<BudgetProfile> { testProfile });
            _budgetRepositoryMock.Setup(repo => repo.GetProfileByNameAsync("TestProfile"))
                .ReturnsAsync(testProfile);
            _budgetRepositoryMock.Setup(repo => repo.GetProfileByNameAsync(It.Is<string>(s => s != "TestProfile")))
                .ReturnsAsync((BudgetProfile?)null);

            _cut = new BudgetService(_budgetRepositoryMock.Object, _transactionUserInteractionMock.Object);
        }

        public void SetupNoProfilesExist()
        {
            _budgetRepositoryMock = new Mock<IBudgetRepository>();
            _budgetRepositoryMock.Setup(repo => repo.LoadBudgetProfilesAsync())
                .ReturnsAsync(new List<BudgetProfile>());
            _budgetRepositoryMock.Setup(repo => repo.GetProfileByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((BudgetProfile?)null);

            _cut = new BudgetService(_budgetRepositoryMock.Object, _transactionUserInteractionMock.Object);
        }

        public void SetupTwoProfilesExist()
        {
            var profile1 = new BudgetProfile(
                name: "Profile1",
                budgetCategories: new Dictionary<string, double>(),
                income: 1000,
                username: "UserA",
                description: "First profile"
            );

            var profile2 = new BudgetProfile(
                name: "Profile2",
                budgetCategories: new Dictionary<string, double>(),
                income: 2000,
                username: "UserB",
                description: "Second profile"
            );

            _budgetRepositoryMock.Setup(repo => repo.LoadBudgetProfilesAsync())
                .ReturnsAsync(new List<BudgetProfile> { profile1, profile2 });
            _budgetRepositoryMock.Setup(repo => repo.GetProfileByNameAsync("Profile1"))
                .ReturnsAsync(profile1);
            _budgetRepositoryMock.Setup(repo => repo.GetProfileByNameAsync("Profile2"))
                .ReturnsAsync(profile2);

            _cut = new BudgetService(_budgetRepositoryMock.Object, _transactionUserInteractionMock.Object);
        }

        [Test]
        public async Task GetProfile_ReturnsBudgetProfile_WhenProfileNameGiven()
        {
            SetupOneProfileExists();

            var result = await _cut.GetProfileAsync("TestProfile");
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task GetProfile_ReturnsNull_WhenProfileWithNameDoesNotExist()
        {
            SetupOneProfileExists();

            var result = await _cut.GetProfileAsync("UnknownProfileName");
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetProfile_ReturnsNull_WhenNoProfilesExist()
        {
            SetupNoProfilesExist();

            var result = await _cut.GetProfileAsync("TestProfile");
            Assert.IsNull(result);
        }

        [Test]
        public async Task StoreProfile_AddsProfileToListAndSaves()
        {
            SetupNoProfilesExist();

            var newProfile = new BudgetProfile(
                name: "NewProfile",
                budgetCategories: new Dictionary<string, double>(),
                income: 2000,
                username: "UserB",
                description: "New budget profile"
            );

            await _cut.StoreProfileAsync(newProfile);

            _budgetRepositoryMock.Verify(repo => repo.SaveBudgetProfileAsync(It.IsAny<BudgetProfile>()), Times.Once);
        }

        [Test]
        public async Task StoreProfile_DoesNotAddDuplicateProfile()
        {
            SetupOneProfileExists();

            var duplicateProfile = new BudgetProfile(
                name: "TestProfile",
                budgetCategories: new Dictionary<string, double>(),
                income: 1500,
                username: "UserA",
                description: "Duplicate profile"
            );

            await _cut.StoreProfileAsync(duplicateProfile);

            _budgetRepositoryMock.Verify(repo => repo.SaveBudgetProfileAsync(It.IsAny<BudgetProfile>()), Times.Never);
        }

        [Test]
        public async Task GetActiveProfile_ReturnsSelectedProfile_WhenUserSelectionValid()
        {
            SetupTwoProfilesExist();

            _transactionUserInteractionMock.Setup(ui => ui.PromptForProfileChoice(It.IsAny<List<string>>()))
                .Returns("Profile1");

            var activeProfile = await _cut.GetActiveProfileAsync();

            Assert.IsNotNull(activeProfile);
            Assert.That(activeProfile.Name, Is.EqualTo("Profile1"));
        }


        [Test]
        public async Task GetActiveProfile_ReturnsNull_WhenUserSelectionIsInvalid()
        {
            SetupTwoProfilesExist();

            _transactionUserInteractionMock.Setup(ui => ui.PromptForProfileChoice(It.IsAny<List<string>>()))
                .Returns((string?)null);

            var activeProfile = await _cut.GetActiveProfileAsync();

            Assert.IsNull(activeProfile);
        }

        [Test]
        public async Task CreateNewProfile_CreatesAndStoresNewProfile_WithValidData()
        {
            SetupNoProfilesExist();

            var newProfile = new BudgetProfile(
                name: "NewProfile",
                budgetCategories: new Dictionary<string, double> { { "Food", 300 }, { "Transport", 200 } },
                income: 2500,
                username: "UserC",
                description: "New budget profile for testing"
            );

            await _cut.StoreProfileAsync(newProfile);

            _budgetRepositoryMock.Verify(repo => repo.SaveBudgetProfileAsync(It.IsAny<BudgetProfile>()), Times.Once);
        }

        [Test]
        public async Task GetBudgetTotal_ReturnsCorrectTotal_WhenProfileExists()
        {
            SetupOneProfileExists();

            var profile = await _cut.GetProfileAsync("TestProfile");

            profile.Categories.Add(new BudgetCategory("Food", 300));
            profile.Categories.Add(new BudgetCategory("Transport", 200));

            double total = profile.BudgetCategories.Values.Sum();
            Assert.That(total, Is.EqualTo(500));
        }
    }
}