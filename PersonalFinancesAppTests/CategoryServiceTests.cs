using Moq;
using PersonalFinances.Repositories;
using PersonalFinances.Models;
using PersonalFinances.App;

namespace PersonalFinancesAppTests
{
    [TestFixture]
    public class CategoryServiceTests
    {
        private Mock<ICategoriesRepository> _categoriesRepositoryMock;
        private Mock<ITransactionsUserInteraction> _transactionUserInteractionMock;
        private CategoriesService _cut;

        [SetUp]
        public void Setup()
        {
            _categoriesRepositoryMock = new Mock<ICategoriesRepository>();
            _transactionUserInteractionMock = new Mock<ITransactionsUserInteraction>();
        }

        [Test]
        public void GetCategory_ReturnsCorrectCategory_WhenVendorMatches()
        {
            var categoriesMap = new Dictionary<string, string>
            {
                { "amazon", "shopping" },
                { "uber", "transportation" }
            };

            _categoriesRepositoryMock.Setup(repo => repo.LoadCategoriesMap()).Returns(categoriesMap);
            var typeDetector = new TransactionTypeDetector();
            _cut = new CategoriesService(_categoriesRepositoryMock.Object, null, _transactionUserInteractionMock.Object, typeDetector);

            var result = _cut.GetCategory("Amazon");

            Assert.That(result, Is.EqualTo("shopping"));
        }

        [Test]
        public void GetCategory_ReturnsEmptyString_WhenVendorDoesNotMatch()
        {
            var categoriesMap = new Dictionary<string, string>
            {
                { "amazon", "shopping" },
                { "uber", "transportation" }
            };

            _categoriesRepositoryMock.Setup(repo => repo.LoadCategoriesMap()).Returns(categoriesMap);
            var typeDetector = new TransactionTypeDetector();
            _cut = new CategoriesService(_categoriesRepositoryMock.Object, null, _transactionUserInteractionMock.Object, typeDetector);

            var result = _cut.GetCategory("UnknownVendor");

            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void GetAllCategories_ReturnsDistinctCategories()
        {
            var categoriesMap = new Dictionary<string, string>
            {
                { "amazon", "shopping" },
                { "uber", "transportation" },
                { "lyft", "transportation" }
            };

            _categoriesRepositoryMock.Setup(repo => repo.LoadCategoriesMap()).Returns(categoriesMap);
            var typeDetector = new TransactionTypeDetector();
            _cut = new CategoriesService(_categoriesRepositoryMock.Object, null, _transactionUserInteractionMock.Object, typeDetector);

            var result = _cut.GetAllCategories();

            Assert.That(result, Is.EquivalentTo(new List<string> { "shopping", "transportation" }));
        }

        [Test]
        public void GetAllCategories_ReturnsEmptyCategories()
        {
            _categoriesRepositoryMock.Setup(repo => repo.LoadCategoriesMap()).Returns(new Dictionary<string, string>());
            var typeDetector = new TransactionTypeDetector();
            _cut = new CategoriesService(_categoriesRepositoryMock.Object, null, _transactionUserInteractionMock.Object, typeDetector);

            var result = _cut.GetAllCategories();
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task StoreNewCategory_AddsCategoryToMap()
        {
            var categoriesMap = new Dictionary<string, string>();
            _categoriesRepositoryMock.Setup(repo => repo.LoadCategoriesMap()).Returns(categoriesMap);
            var typeDetector = new TransactionTypeDetector();
            _cut = new CategoriesService(_categoriesRepositoryMock.Object, null, _transactionUserInteractionMock.Object, typeDetector);

            await _cut.StoreNewCategoryAsync("netflix", "entertainment");

            _categoriesRepositoryMock.Verify(repo => repo.SaveCategoriesMap(categoriesMap), Times.Once);
            Assert.That(categoriesMap, Contains.Key("netflix"));
            Assert.That(categoriesMap["netflix"], Is.EqualTo("entertainment"));
        }


    }
}