using PersonalFinances.Repositories;
using PersonalFinances.App;
using PersonalFinances.Models;


string vendersJsonPath = "";
string categoriesJsonPath = "";
string BudgetProfilesJsonPath = "";
string currentProfile = "";

// Transactions Paths
var transactionsDictionary = new Dictionary<string, Type>
{
    { "", typeof(RBCTransaction) },
    { "", typeof(AmexTransaction) } 
};

TransactionFilterService.TransactionRange transactionRange = TransactionFilterService.TransactionRange.All;

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