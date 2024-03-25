using PersonalFinances.Repositories;
using PersonalFinances.App;

string testTransactionsCsvPath = "";
string vendersJsonPath = "./vendors.json";
string categoriesJsonPath = "./categories.json";
string BudgetProfilesJsonPath = "./budgetProfiles.json";
var TransactionsConsoleUserInteraction = new TransactionsConsoleUserInteraction();

var FinancesApp = new PersonalFinancesApp(
    new CsvTransactionRepository(),
    TransactionsConsoleUserInteraction,
    new VendorsService(
        new VendorsRepository(vendersJsonPath),
        TransactionsConsoleUserInteraction),
    new CategoriesService(
        new CategoriesRepository(categoriesJsonPath),
        TransactionsConsoleUserInteraction),
    new BudgetService(
        new BudgetRepository(BudgetProfilesJsonPath),
        TransactionsConsoleUserInteraction)
);

FinancesApp.Run(testTransactionsCsvPath);