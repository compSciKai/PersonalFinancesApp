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
    new BudgetService(
        new BudgetRepository(BudgetProfilesJsonPath),
        TransactionsConsoleUserInteraction)
);

await FinancesApp.RunAsync(transactionsDictionary, transactionRange, currentProfile);
