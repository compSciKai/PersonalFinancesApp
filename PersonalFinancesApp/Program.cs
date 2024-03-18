using PersonalFinances.Repositories;
using PersonalFinances.App;

// Initialize appliction
var FinancialApp = new PersonalFinancesApp(
    new CsvTransactionRepository(),
    new TransactionsConsoleUserInteraction()
);

// TODO: add path to existing transactions
string testTransactionsCsv = "";
FinancialApp.Run(testTransactionsCsv);



// // Categorize Transactions
// FinancialApp.OutputCategorized(transactions);