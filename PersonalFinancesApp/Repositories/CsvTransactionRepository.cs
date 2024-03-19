using PersonalFinances.Models;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;

namespace PersonalFinances.Repositories;

public class CsvTransactionRepository : ITransactionsRepository
{
    public List<Transaction> GetTransactions(string filePath)
    {
        // Logic to read transactions from the CSV file
        // and convert them into a list of Transaction objects.
        // var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        // {

        // }
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        };

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, config))
        {
            var transactionEnumerable = csv.GetRecords<Transaction>();
            
            return transactionEnumerable.ToList();
        }
    }
}
