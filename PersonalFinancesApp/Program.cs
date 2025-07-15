using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Options;
using PersonalFinances.App;
using PersonalFinances.Data;
using PersonalFinances.Models;
using PersonalFinances.Repositories;
using System;
using System.Collections.Generic;

//string vendersJsonPath = "";
//string categoriesJsonPath = "";
//string BudgetProfilesJsonPath = "";
//string currentProfile = "";

// Transactions Paths
//var transactionsDictionary = new Dictionary<string, Type>
//{
//    { "", typeof(RBCTransaction) },
//    { "", typeof(AmexTransaction) }
//};

//TransactionFilterService.TransactionRange transactionRange = TransactionFilterService.TransactionRange.LastMonth;

//var TransactionsConsoleUserInteraction = new TransactionsConsoleUserInteraction();

//var FinancesApp = new PersonalFinancesApp(
//    new CsvTransactionRepository(),
//    TransactionsConsoleUserInteraction,
//    new VendorsService(
//        new VendorsRepository(vendersJsonPath),
//        TransactionsConsoleUserInteraction),
//    new CategoriesService(
//        new CategoriesRepository(categoriesJsonPath),
//        TransactionsConsoleUserInteraction),
//    new BudgetService(
//        new BudgetRepository(BudgetProfilesJsonPath),
//        TransactionsConsoleUserInteraction)
//);

//FinancesApp.Run(transactionsCsvPath, transactionRange, currentProfile);

TransactionContext entities = new TransactionContext();

Transaction testTransaction = new RBCTransaction()
{
    AccountType = "RBC",
    Description1 = "Test.",
    Date = DateTime.Now,
    Amount = 50.05m,
    Description2 = "This is a test"
};


using (var context = new TransactionContext())
{
    bool canConnect = context.Database.CanConnect();
    Console.WriteLine($"Can connect: {canConnect}");

    if (canConnect)
    {
        // Try to ensure database is created
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }
}


entities.Add(testTransaction);
entities.SaveChanges();

IEnumerable<Transaction> transactions = entities.Transactions.ToList<Transaction>();

foreach (Transaction transaction in transactions)
{
    Console.WriteLine(transaction.Description);
}

//FinancesApp.Run(transactionsDictionary, transactionRange, currentProfile);