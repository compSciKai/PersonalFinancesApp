using PersonalFinances.Repositories;
using PersonalFinances.App;

string transactionsCsvPath = "";
string vendersJsonPath = "";
string categoriesJsonPath = "";
string BudgetProfilesJsonPath = "";
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

FinancesApp.Run(transactionsCsvPath, transactionRange);