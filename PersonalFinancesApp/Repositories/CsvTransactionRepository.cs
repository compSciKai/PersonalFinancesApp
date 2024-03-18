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
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<Transaction>();
            foreach (var record in records) 
            {
                Console.WriteLine($"{record.Date}: {record.Description} - {record.Amount}");
            }
        }

        return new List<Transaction>();

    }
}
