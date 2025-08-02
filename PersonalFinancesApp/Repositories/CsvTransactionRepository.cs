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
            // Read header to detect if this is an RBC CSV
            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord;

            // Check if it has RBC-style columns
            bool isRBCFormat = headers.Contains("Description 1") && headers.Contains("Description 2");

            if (typeof(T) == typeof(RBCTransaction) && isRBCFormat)
            {
                csv.Context.RegisterClassMap<RBCTransactionMap>();
            }

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

    Task<int> ITransactionSavable.SaveTransactionsWithHashAsync<T>(List<T> transactions)
    {
        throw new NotImplementedException();
    }

    public class RBCTransactionMap : ClassMap<RBCTransaction>
    {
        public RBCTransactionMap()
        {
            Map(m => m.AccountType).Name("Account Type");
            Map(m => m.Date).Name("Transaction Date");
            Map(m => m.Amount).Name("CAD$");

            // Custom mapping for Description - combine Description 1 and Description 2
            Map(m => m.Description).Convert(args =>
            {
                var desc1 = args.Row.GetField("Description 1") ?? "";
                var desc2 = args.Row.GetField("Description 2") ?? "";
                return $"{desc1} {desc2}".Trim();
            });
        }
    }
}
