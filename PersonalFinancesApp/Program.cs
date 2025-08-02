using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Options;
using PersonalFinances.App;
using PersonalFinances.Data;
using PersonalFinances.Models;
using PersonalFinances.Repositories;
using System;
using System.Collections.Generic;

string vendersJsonPath = "";
string categoriesJsonPath = "";
string BudgetProfilesJsonPath = "";
string currentProfile = "";

// Transactions Paths
var transactionsDictionary = new Dictionary<string, Type>
{
    { "", typeof(RBCTransaction) },
    { "", typeof(AmexTransaction) }
};

TransactionFilterService.TransactionRange transactionRange = TransactionFilterService.TransactionRange.LastMonth;

var TransactionsConsoleUserInteraction = new TransactionsConsoleUserInteraction();
var entities = new TransactionContext();

var FinancesApp = new PersonalFinancesApp(
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
    new BudgetService(
        new BudgetRepository(BudgetProfilesJsonPath),
        TransactionsConsoleUserInteraction)
);

await FinancesApp.RunAsync(transactionsDictionary, transactionRange, currentProfile);

//TransactionContext entities = new TransactionContext();

//Transaction rbcTestTransaction = new RBCTransaction()
//{
//    Description = "Test.",
//    Date = DateTime.Now,
//    Amount = 50.05m,
//};

//Transaction amexTestTransaction = new AmexTransaction()
//{
//    Description = "Test.",
//    Date = DateTime.Now,
//    Amount = 50.05m,
//    AccountNumber = "123456",
//    MemberName = "John Doe"
//};

//Transaction pcTestTransaction = new PCFinancialTransaction()
//{
//    Description = "Test.",
//    Date = DateTime.Now,
//    Amount = 50.05m,
//    MemberName = "John Doe",
//    TransactionType = "PC Financial"
//};

//using (var context = new TransactionContext())
//{
//    bool canConnect = context.Database.CanConnect();
//    Console.WriteLine($"Can connect: {canConnect}");

//    if (canConnect)
//    {
//        // Try to ensure database is created
//        //context.Database.EnsureDeleted();
//        context.Database.EnsureCreated();
//        // ensure table created

//    }
//}


//entities.Add(rbcTestTransaction);
//entities.Add(amexTestTransaction);
//entities.Add(pcTestTransaction);
//entities.SaveChanges();

//IEnumerable<Transaction> rbcTransactions = entities.RBCTransactions.ToList<Transaction>();

//foreach (Transaction transaction in rbcTransactions)
//{
//    Console.WriteLine(transaction.Description);
//}

