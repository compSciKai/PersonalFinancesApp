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

Transaction testTransaction = new Transaction()
{
    AccountType = "credit",
    AccountNumber = "05",
    Date = DateTime.Now,
    Amount = 50.05m,
    Description1 = "This is a test"
};



TransactionFilterService.TransactionRange transactionRange = TransactionFilterService.TransactionRange.All;
entities.Add(testTransaction);
entities.SaveChanges();

IEnumerable<Transaction> transactions = entities.TransactionItem.ToList<Transaction>();

foreach (Transaction transaction in transactions)
{
    Console.WriteLine(transaction.Description1);
}

//FinancesApp.Run(transactionsDictionary, transactionRange, currentProfile);