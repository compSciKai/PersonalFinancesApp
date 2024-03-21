using PersonalFinances.Repositories;
using PersonalFinances.App;

string testTransactionsCsvPath = "";
string vendersJsonPath = "";
var TransactionsConsoleUserInteraction = new TransactionsConsoleUserInteraction();

var FinancesApp = new PersonalFinancesApp(
    new CsvTransactionRepository(),
    TransactionsConsoleUserInteraction,
    new VendorsService(
        new VendorsRepository(vendersJsonPath),
        TransactionsConsoleUserInteraction)
);

FinancesApp.Run(testTransactionsCsvPath);