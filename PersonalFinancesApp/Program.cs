using PersonalFinances.App;
using PersonalFinances.Data;
using PersonalFinances.Models;
using PersonalFinances.Repositories;
using PersonalFinances.Utilities;
using Microsoft.Extensions.Configuration;


Console.OutputEncoding = System.Text.Encoding.UTF8;

// Transactions Paths
//var transactionsDictionary = new Dictionary<string, Type>
//{
//    { @"", typeof(RBCTransaction) },
//    { @"", typeof(AmexTransaction) }
//};

//Dictionary<string, Type> transactionsDictionary = null;
   

TransactionFilterService.TransactionRange transactionRange = TransactionFilterService.TransactionRange.CurrentMonth;

// Build configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

// Load transfer management settings
var transferSettings = new TransferManagementSettings();
configuration.GetSection("TransferManagement").Bind(transferSettings);

// Initialize database context with configuration
TransactionContext.Initialize(configuration);

var TransactionsConsoleUserInteraction = new TransactionsConsoleUserInteraction();
var entities = new TransactionContext();

Console.WriteLine();
Console.WriteLine();
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

// Validate database connection FIRST before any services try to access it
try
{
    Console.WriteLine("Checking database connection...");
    await entities.ValidateDatabaseConnectionAsync();
    Console.WriteLine("Database connection successful!\n");
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return;
}

// Initialize repositories and services with error handling
DatabaseVendorsRepository vendorsDbRepo;
DatabaseCategoriesRepository categoriesDbRepo;
TransactionTypeDetector typeDetector;
BudgetService budgetService;
VendorsService vendorsService;
CategoriesService categoriesService;
TransferManagementService transferManagementService;
TransactionReprocessingService reprocessingService;

try
{
    vendorsDbRepo = new DatabaseVendorsRepository(entities);
    categoriesDbRepo = new DatabaseCategoriesRepository(entities);
    typeDetector = new TransactionTypeDetector();

    budgetService = new BudgetService(
        new DatabaseBudgetRepository(entities),
        TransactionsConsoleUserInteraction,
        null);

    vendorsService = new VendorsService(
        vendorsDbRepo,
        null,
        TransactionsConsoleUserInteraction);

    categoriesService = new CategoriesService(
        categoriesDbRepo,
        null,
        TransactionsConsoleUserInteraction,
        typeDetector);

    transferManagementService = new TransferManagementService(
        TransactionsConsoleUserInteraction,
        new SqlServerTransactionRepository<RBCTransaction>(entities),
        new SqlServerTransactionRepository<AmexTransaction>(entities),
        new SqlServerTransactionRepository<PCFinancialTransaction>(entities),
        categoriesService,
        transferSettings);

    reprocessingService = new TransactionReprocessingService(
        new SqlServerTransactionRepository<RBCTransaction>(entities),
        new SqlServerTransactionRepository<AmexTransaction>(entities),
        new SqlServerTransactionRepository<PCFinancialTransaction>(entities),
        vendorsService,
        categoriesService,
        budgetService);
}
catch (Npgsql.PostgresException ex) when (ex.SqlState == "28P01" || ex.Message.Contains("password authentication failed"))
{
    Console.Error.WriteLine(BoxFormatter.CreateErrorBox(
        "AUTHENTICATION FAILED",
        "The database password is incorrect or has been changed.",
        "",
        "To fix this:",
        "  1. Open PersonalFinancesApp/appsettings.json",
        "  2. Find the line with 'Password=YOUR_NEW_PASSWORD_HERE'",
        "  3. Replace YOUR_NEW_PASSWORD_HERE with your actual password",
        "  4. Save the file and run the application again",
        "",
        "Need help? See PASSWORD_UPDATE_GUIDE.md"
    ));
    return;
}
catch (Npgsql.NpgsqlException ex) when (ex.Message.Contains("password authentication failed"))
{
    Console.Error.WriteLine(BoxFormatter.CreateErrorBox(
        "AUTHENTICATION FAILED",
        "The database password is incorrect.",
        "",
        "To fix this:",
        "  1. Open PersonalFinancesApp/appsettings.json",
        "  2. Update the password in the connection string",
        "  3. Save and run again",
        "",
        "See PASSWORD_UPDATE_GUIDE.md for detailed instructions"
    ));
    return;
}
catch (Exception ex)
{
    Console.Error.WriteLine(BoxFormatter.CreateErrorBox(
        "DATABASE INITIALIZATION FAILED",
        $"Error: {ex.Message}",
        "",
        "This could be due to:",
        "  - Missing or incorrect appsettings.json configuration",
        "  - Database connection issues",
        "  - Invalid credentials"
    ));
    return;
}

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

await FinancesApp.RunAsync(transactionsDictionary, null);