using PersonalFinances.Repositories;
using PersonalFinances.App;

string testTransactionsCsvPath = "";
string vendersJsonPath = "";
string categoriesJsonPath = "";
var TransactionsConsoleUserInteraction = new TransactionsConsoleUserInteraction();

var FinancesApp = new PersonalFinancesApp(
    new CsvTransactionRepository(),
    TransactionsConsoleUserInteraction,
    new VendorsService(
        new VendorsRepository(vendersJsonPath),
        TransactionsConsoleUserInteraction),
    new CategoriesService(
        new CategoriesRepository(categoriesJsonPath),
        TransactionsConsoleUserInteraction)
);

FinancesApp.Run(testTransactionsCsvPath);