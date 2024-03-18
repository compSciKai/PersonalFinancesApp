

// Initialize appliction
var FinancialApp = new PersonalFinancesApp();
FinancialApp.Init();

// Get Transactions
var transactions = FinancialApp.GetTransactions(Path);

// Categorize Transactions
FinancialApp.OutputCategorized(transactions);