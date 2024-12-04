using PersonalFinances.Repositories;
using PersonalFinances.App;
using PersonalFinances.Models;
using PersonalFinances.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore.Query.Internal;

//string transactionsCsvPath = "";
//string vendersJsonPath = "";
//string categoriesJsonPath = "";
//string BudgetProfilesJsonPath = "";
//string currentProfile = "";

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
    Amount = 50.05f,
    Description1 = "This is a test"
};

entities.Add(testTransaction);
entities.SaveChanges();

IEnumerable<Transaction> transactions = entities.TransactionItem.ToList<Transaction>();

foreach (Transaction transaction in transactions)
{
    Console.WriteLine(transaction.Description1);
}

