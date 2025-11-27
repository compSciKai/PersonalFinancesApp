using PersonalFinances.Models;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;

namespace PersonalFinances.Repositories;

public class CsvTransactionRepository<T> : IFileTransactionRepository<T> where T : Transaction
{


    public async Task<List<T>> LoadFromFileAsync(string filePath)
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
            // Read header to detect the CSV format
            await csv.ReadAsync();
            csv.ReadHeader();
            var headers = csv.HeaderRecord;

            // Check if it has RBC-style columns
            bool isRBCFormat = headers.Contains("Description 1") && headers.Contains("Description 2");
            // Check if it has PC Financial-style columns
            bool isPCFormat = headers.Contains("Type") && headers.Contains("Card Holder Name");

            if (typeof(T) == typeof(RBCTransaction) && isRBCFormat)
            {
                csv.Context.RegisterClassMap<RBCTransactionMap>();
            }
            else if (typeof(T) == typeof(PCFinancialTransaction) && isPCFormat)
            {
                csv.Context.RegisterClassMap<PCFinancialTransactionMap>();
            }

            var transactions = new List<T>();
            await foreach (var record in csv.GetRecordsAsync<T>())
            {
                transactions.Add(record);
            }
            return transactions;
        }
    }

    public async Task SaveToFileAsync(List<T> transactions, string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        using (var writer = new StreamWriter(filePath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            await csv.WriteRecordsAsync(transactions);
        }
    }

    public async Task<List<T>> GetAllAsync()
    {
        throw new NotSupportedException("CSV repository requires a file path. Use LoadFromFileAsync instead.");
    }

    public async Task<List<T>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        throw new NotSupportedException("CSV repository requires a file path. Use LoadFromFileAsync and filter the results.");
    }

    public async Task<int> SaveAsync(List<T> transactions)
    {
        throw new NotSupportedException("CSV repository requires a file path. Use SaveToFileAsync instead.");
    }

    public async Task<int> UpdateAsync(List<T> transactions)
    {
        throw new NotSupportedException("CSV repository does not support updating transactions. Use SaveToFileAsync to overwrite the entire file.");
    }

    public async Task<bool> ExistsAsync(string transactionHash)
    {
        throw new NotSupportedException("CSV repository cannot check existence without loading a file first.");
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

    public class PCFinancialTransactionMap : ClassMap<PCFinancialTransaction>
    {
        public PCFinancialTransactionMap()
        {
            Map(m => m.Date).Name("Date");
            Map(m => m.Description).Name("Description");
            Map(m => m.Amount).Name("Amount");
            Map(m => m.MemberName).Name("Card Holder Name");
            Map(m => m.TransactionType).Name("Type"); // This is the string property
            Map(m => m.AccountType).Ignore(); // Set in the class default
            Map(m => m.Type).Ignore(); // Ignore the enum Type from base class during CSV reading
        }
    }
}
