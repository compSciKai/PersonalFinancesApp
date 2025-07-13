using PersonalFinances.Repositories;
using PersonalFinances.App;


string vendersJsonPath = "";
string categoriesJsonPath = "";
string BudgetProfilesJsonPath = "";
string currentProfile = "";

// Transactions Paths
var transactionsDictionary = new Dictionary<string, Type>
{
    {"path/transactions.csv", typeof(Transaction) }
};

TransactionFilterService.TransactionRange transactionRange = TransactionFilterService.TransactionRange.LastMonth;

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

FinancesApp.Run(transactionsDictionary, transactionRange, currentProfile);