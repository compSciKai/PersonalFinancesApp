using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Options;
using PersonalFinances.App;
using PersonalFinances.Data;
using PersonalFinances.Models;
using PersonalFinances.Repositories;
using System;
using System.Collections.Generic;

string vendersJsonPath = @"";
string categoriesJsonPath = @"";
string BudgetProfilesJsonPath = @"";
string currentProfile = "";

// Transactions Paths
var transactionsDictionary = new Dictionary<string, Type>
{
    { @"", typeof(RBCTransaction) },
    { @"", typeof(AmexTransaction) }
};

TransactionFilterService.TransactionRange transactionRange = TransactionFilterService.TransactionRange.All;

var TransactionsConsoleUserInteraction = new TransactionsConsoleUserInteraction();
var entities = new TransactionContext();

var budgetService = new BudgetService(
    new DatabaseBudgetRepository(entities),
    TransactionsConsoleUserInteraction,
    new BudgetRepository(BudgetProfilesJsonPath));

// Check for migration command
if (args.Length > 0 && args[0] == "migrate-profiles")
{
    Console.WriteLine("Running profile migration from JSON to database...\n");
    await budgetService.MigrateProfilesToDatabaseAsync();
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
    new VendorsService(
        new VendorsRepository(vendersJsonPath),
        TransactionsConsoleUserInteraction),
    new CategoriesService(
        new CategoriesRepository(categoriesJsonPath),
        TransactionsConsoleUserInteraction),
    budgetService
);

await FinancesApp.RunAsync(transactionsDictionary, transactionRange, currentProfile);