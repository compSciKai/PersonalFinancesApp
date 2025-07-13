using PersonalFinances.Models;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;

namespace PersonalFinances.Repositories;

public class CsvTransactionRepository : ITransactionsRepository
{
    public List<T> GetTransactions<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file at path '{filePath}' does not exist.");
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        };

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, config))
        {
            var transactionEnumerable = csv.GetRecords<T>();
            return transactionEnumerable.ToList();
        }
    }

    public void ExportTransactions(List<Transaction> transactions, string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        using (var writer = new StreamWriter(filePath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(transactions);
        }
    }
}
