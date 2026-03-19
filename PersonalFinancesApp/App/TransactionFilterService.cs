using PersonalFinances.Models;
using System.ComponentModel;
using System.Reflection;

namespace PersonalFinances.App;

public static class TransactionFilterService
{
    public static List<Transaction> GetTransactionsInRange(List<Transaction> transactions, TransactionRange? filterString) {
        if (filterString == TransactionRange.CurrentMonth) {
            return transactions.Where(
                transaction => transaction.Date > LastDayOfLastMonth()
            ).ToList();
        } 
        else if (filterString == TransactionRange.LastMonth) {
            return transactions.Where(
                transaction => transaction.Date > FirstDayOfLastMonth() && transaction.Date < LastDayOfLastMonth()
            ).ToList();
        }
        else if (filterString == TransactionRange.Last3Months) {
            DateTime threeMonthsAgo = FirstDayOfThreeMonthsAgo();
            return transactions.Where(
                transaction => transaction.Date >= threeMonthsAgo
            ).ToList();
        }
        else if (filterString == TransactionRange.All)
        {
            return transactions;
        }

        return transactions;
    }

    public static List<Transaction> GetTransactionsForUser(List<Transaction> transactions, string userName)
    {
        if (string.IsNullOrEmpty(userName))
        {
            return transactions;
        }

        return transactions.Where(transaction => transaction.MemberName.ToLower().Contains(userName.ToLower()) || transaction.MemberName == string.Empty).ToList();
    }

    private static DateTime LastDayOfLastMonth() 
    {
        DateTime firstDayofThisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0);
        return firstDayofThisMonth.AddSeconds(-1);
    }
    
    private static DateTime FirstDayOfThisMonth()
    {
        return new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0);
    }

    private static DateTime FirstDayOfThreeMonthsAgo()
    {
        DateTime firstDayOfThisMonth = FirstDayOfThisMonth();
        return firstDayOfThisMonth.AddMonths(-3);
    }

    private static DateTime FirstDayOfLastMonth()
    {
        DateTime firstDayOfThisMonth = FirstDayOfThisMonth();
        return firstDayOfThisMonth.AddMonths(-1);
    }

    public enum TransactionRange
    {
        [Description("Current Month")]
        CurrentMonth,
        [Description("Last Month")]
        LastMonth,
        [Description("Last Three Months")]
        Last3Months,
        [Description("All")]
        All,
    }

    public static string GetHumanReadableTransactionRange(TransactionRange? value)
    {
        FieldInfo field = value?.GetType().GetField(value.ToString());
        DescriptionAttribute attribute = (DescriptionAttribute)field?.GetCustomAttribute(typeof(DescriptionAttribute));

        return attribute == null ? value.ToString() : attribute?.Description;
    }
}
