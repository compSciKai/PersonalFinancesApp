using PersonalFinances.Repositories;
using PersonalFinances.App;

var FinancesApp = new PersonalFinancesApp(
    new CsvTransactionRepository(),
    new TransactionsConsoleUserInteraction(),
    new VendorsService(
        new VendorsRepository())
);

string testTransactionsCsvPath = "";
string? vendersJsonPath = "";
FinancesApp.Run(testTransactionsCsvPath, vendersJsonPath);