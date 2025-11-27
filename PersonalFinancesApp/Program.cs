using PersonalFinances.App;
using PersonalFinances.Data;
using PersonalFinances.Models;
using PersonalFinances.Repositories;
using PersonalFinances.Utilities;


// Transactions Paths
//var transactionsDictionary = new Dictionary<string, Type>
//{
//    { @"", typeof(RBCTransaction) },
//    { @"", typeof(AmexTransaction) }
//};

//Dictionary<string, Type> transactionsDictionary = null;
   

TransactionFilterService.TransactionRange transactionRange = TransactionFilterService.TransactionRange.CurrentMonth;

var TransactionsConsoleUserInteraction = new TransactionsConsoleUserInteraction();
var entities = new TransactionContext();

// Validate database connection before proceeding
try
{
    Console.WriteLine("Checking database connection...");
    await entities.ValidateDatabaseConnectionAsync();
    Console.WriteLine("Database connection successful!\n");
    Console.WriteLine("  /$$$$$$                      /$$       /$$$$$$$$ /$$                        ");
    Console.WriteLine(" /$$__  $$                    | $$      | $$_____/| $$                        ");
    Console.WriteLine("| $$  \\__/  /$$$$$$   /$$$$$$$| $$$$$$$ | $$      | $$  /$$$$$$  /$$  /$$  /$$");
    Console.WriteLine("| $$       |____  $$ /$$_____/| $$__  $$| $$$$$   | $$ /$$__  $$| $$ | $$ | $$");
    Console.WriteLine("| $$        /$$$$$$$|  $$$$$$ | $$  \\ $$| $$__/   | $$| $$  \\ $$| $$ | $$ | $$");
    Console.WriteLine("| $$    $$ /$$__  $$ \\____  $$| $$  | $$| $$      | $$| $$  | $$| $$ | $$ | $$");
    Console.WriteLine("|  $$$$$$/|  $$$$$$$ /$$$$$$$/| $$  | $$| $$      | $$|  $$$$$$/|  $$$$$/$$$$/");
    Console.WriteLine(" \\______/  \\_______/|_______/ |__/  |__/|__/      |__/ \\______/  \\_____/\\___/ ");
    Console.WriteLine();
    Console.WriteLine();
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
    return;
}

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

var transferManagementService = new TransferManagementService(
    TransactionsConsoleUserInteraction,
    new SqlServerTransactionRepository<RBCTransaction>(entities),
    new SqlServerTransactionRepository<AmexTransaction>(entities),
    new SqlServerTransactionRepository<PCFinancialTransaction>(entities),
    categoriesService);

var reprocessingService = new TransactionReprocessingService(
    new SqlServerTransactionRepository<RBCTransaction>(entities),
    new SqlServerTransactionRepository<AmexTransaction>(entities),
    new SqlServerTransactionRepository<PCFinancialTransaction>(entities),
    vendorsService,
    categoriesService,
    budgetService);

// Check for utility commands
if (args.Length > 0)
{
    switch (args[0])
    {
        case "migrate-mappings":
            Console.WriteLine("Running vendor and category mapping migration from JSON to database...\n");
            await vendorsService.MigrateVendorsFromJsonAsync();
            await categoriesService.MigrateCategoriesFromJsonAsync();
            Console.WriteLine("\nMigration complete.");
            return;

        case "fix-duplicates":
            var duplicateDetector = new CategoryDuplicateDetector(entities);
            bool? autoResolve = args.Length > 1 && args[1] == "--auto" ? true : (bool?)null;
            await duplicateDetector.DetectAndResolveInteractiveAsync(autoResolve);
            return;

        case "help":
        case "--help":
        case "-h":
            Console.WriteLine("CashFlow - Personal Finance Application");
            Console.WriteLine("\nUsage: dotnet run [command] [options]\n");
            Console.WriteLine("Commands:");
            Console.WriteLine("  migrate-mappings           Migrate vendors and categories from JSON to database");
            Console.WriteLine("  fix-duplicates [--auto]    Detect and fix duplicate category names");
            Console.WriteLine("                             --auto: Automatically resolve without prompting");
            Console.WriteLine("  help                       Show this help message\n");
            Console.WriteLine("Run without arguments to start the normal application.\n");
            return;

        default:
            Console.WriteLine($"Unknown command: {args[0]}");
            Console.WriteLine("Run 'dotnet run help' for available commands.\n");
            return;
    }
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
    budgetService,
    transferManagementService,
    reprocessingService
);

await FinancesApp.RunAsync(transactionsDictionary, transactionRange);