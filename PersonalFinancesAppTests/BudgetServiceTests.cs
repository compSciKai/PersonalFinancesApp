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
            _budgetRepositoryMock.Setup(repo => repo.LoadBudgetProfiles())
                .Returns(new List<BudgetProfile>
                {
                    new BudgetProfile(
                        name: "TestProfile",
                        budgetCategories: new Dictionary<string, double>(),
                        income: 1000,
                        username: "UserA",
                        description: "Mock budget profile"
                    )
                });

            _cut = new BudgetService(_budgetRepositoryMock.Object, _transactionUserInteractionMock.Object);
        }

        public void SetupNoProfilesExist()
        {
            _budgetRepositoryMock = new Mock<IBudgetRepository>();
            _budgetRepositoryMock.Setup(repo => repo.LoadBudgetProfiles())
                .Returns(new List<BudgetProfile>());

            _cut = new BudgetService(_budgetRepositoryMock.Object, _transactionUserInteractionMock.Object);
        }

        public void SetupTwoProfilesExist()
        {
            _budgetRepositoryMock.Setup(repo => repo.LoadBudgetProfiles())
                .Returns(new List<BudgetProfile>
                {
                    new BudgetProfile(
                        name: "Profile1",
                        budgetCategories: new Dictionary<string, double>(),
                        income: 1000,
                        username: "UserA",
                        description: "First profile"
                    ),
                    new BudgetProfile(
                        name: "Profile2",
                        budgetCategories: new Dictionary<string, double>(),
                        income: 2000,
                        username: "UserB",
                        description: "Second profile"
                    )
                });

            _cut = new BudgetService(_budgetRepositoryMock.Object, _transactionUserInteractionMock.Object);
        }

        [Test]
        public void GetProfile_ReturnsBudgetProfile_WhenProfileNameGiven()
        {
            SetupOneProfileExists();

            Assert.IsNotNull(_cut.GetProfile("TestProfile"));
        }

        [Test]
        public void GetProfile_ReturnsNull_WhenProfileWithNameDoesNotExist()
        {
            SetupOneProfileExists();

            Assert.IsNull(_cut.GetProfile("UnknownProfileName"));
        }

        [Test]
        public void GetProfile_ReturnsNull_WhenNoProfilesExist()
        {
            SetupNoProfilesExist();

            Assert.IsNull(_cut.GetProfile("TestProfile"));
        }

        [Test]
        public void StoreProfile_AddsProfileToListAndSaves()
        {
            SetupNoProfilesExist();

            var newProfile = new BudgetProfile(
                name: "NewProfile",
                budgetCategories: new Dictionary<string, double>(),
                income: 2000,
                username: "UserB",
                description: "New budget profile"
            );

            _cut.StoreProfile(newProfile);

            _budgetRepositoryMock.Verify(repo => repo.SaveBudgetProfiles(It.IsAny<List<BudgetProfile>>()), Times.Once);
            Assert.IsNotNull(_cut.GetProfile("NewProfile"));
        }

        [Test]
        public void StoreProfile_DoesNotAddDuplicateProfile()
        {
            SetupOneProfileExists();

            var duplicateProfile = new BudgetProfile(
                name: "TestProfile",
                budgetCategories: new Dictionary<string, double>(),
                income: 1500,
                username: "UserA",
                description: "Duplicate profile"
            );

            _cut.StoreProfile(duplicateProfile);

            Assert.That(_cut.BudgetProfiles.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetActiveProfile_ReturnsSelectedProfile_WhenUserSelectionValid()
        {
            SetupTwoProfilesExist();

            _transactionUserInteractionMock.Setup(ui => ui.PromptForProfileChoice(It.IsAny<List<string>>()))
                .Returns("Profile1");

            var activeProfile = _cut.GetActiveProfile();

            Assert.IsNotNull(activeProfile);
            Assert.That(activeProfile.Name, Is.EqualTo("Profile1"));
        }


        [Test]
        public void GetActiveProfile_ReturnsNull_WhenUserSelectionIsInvalid()
        {
            SetupTwoProfilesExist();

            _transactionUserInteractionMock.Setup(ui => ui.PromptForProfileChoice(It.IsAny<List<string>>()))
                .Returns((string?)null);

            var activeProfile = _cut.GetActiveProfile();

            Assert.IsNull(activeProfile);
        }

        [Test]
        public void CreateNewProfile_CreatesAndStoresNewProfile_WithValidData()
        {
            SetupNoProfilesExist();

            var newProfile = new BudgetProfile(
                name: "NewProfile",
                budgetCategories: new Dictionary<string, double> { { "Food", 300 }, { "Transport", 200 } },
                income: 2500,
                username: "UserC",
                description: "New budget profile for testing"
            );

            _cut.StoreProfile(newProfile);

            _budgetRepositoryMock.Verify(repo => repo.SaveBudgetProfiles(It.IsAny<List<BudgetProfile>>()), Times.Once);
            Assert.IsNotNull(_cut.GetProfile("NewProfile"));
        }

        [Test]
        public void GetBudgetTotal_ReturnsCorrectTotal_WhenProfileExists()
        {
            SetupOneProfileExists();

            var profile = _cut.GetProfile("TestProfile");

            profile.BudgetCategories.Add("Food", 300);
            profile.BudgetCategories.Add("Transport", 200);

            double total = profile.BudgetCategories.Values.Sum();
            Assert.That(total, Is.EqualTo(500));
        }
    }
}