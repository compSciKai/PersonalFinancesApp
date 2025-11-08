using PersonalFinances.App;
using PersonalFinances.Data;
using PersonalFinances.Models;
using PersonalFinances.Repositories;


// Transactions Paths
//var transactionsDictionary = new Dictionary<string, Type>
//{
//    { @"", typeof(RBCTransaction) },
//    { @"", typeof(AmexTransaction) }
//};

Dictionary<string, Type> transactionsDictionary = null;
   

TransactionFilterService.TransactionRange transactionRange = TransactionFilterService.TransactionRange.LastMonth;

var TransactionsConsoleUserInteraction = new TransactionsConsoleUserInteraction();
var entities = new TransactionContext();

// Initialize repositories
var vendorsDbRepo = new DatabaseVendorsRepository(entities);
var categoriesDbRepo = new DatabaseCategoriesRepository(entities);

// Initialize transaction type detector
var typeDetector = new TransactionTypeDetector();

// Initialize services
var budgetService = new BudgetService(
    new DatabaseBudgetRepository(entities),
    TransactionsConsoleUserInteraction,
    null); // JSON repository removed for budget profiles

var vendorsService = new VendorsService(
    vendorsDbRepo,
    null,
    TransactionsConsoleUserInteraction);

var categoriesService = new CategoriesService(
    categoriesDbRepo,
    null,
    TransactionsConsoleUserInteraction,
    typeDetector);

// Check for migration commands
if (args.Length > 0 && args[0] == "migrate-mappings")
{
    Console.WriteLine("Running vendor and category mapping migration from JSON to database...\n");
    await vendorsService.MigrateVendorsFromJsonAsync();
    await categoriesService.MigrateCategoriesFromJsonAsync();
    Console.WriteLine("\nMigration complete. Press any key to exit...");
    Console.ReadKey();
    return;
}

var FinancesApp = new PersonalFinances.App.PersonalFinancesApp(
    new CsvTransactionRepository<RBCTransaction>(),
    new CsvTransactionRepository<AmexTransaction>(),
    new CsvTransactionRepository<PCFinancialTransaction>(),
    new SqlServerTransactionRepository<RBCTransaction>(entities),
    new SqlServerTransactionRepository<AmexTransaction>(entities),
    new SqlServerTransactionRepository<PCFinancialTransaction>(entities),
    TransactionsConsoleUserInteraction,
    vendorsService,
    categoriesService,
    budgetService
);

await FinancesApp.RunAsync(transactionsDictionary, transactionRange);