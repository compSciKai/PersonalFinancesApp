using PersonalFinances.Models;

namespace PersonalFinances.App;

public interface ICsvFileArchiveService
{
    Task<bool> ArchiveFileAsync(string sourceFilePath, string? processedFolderPath, List<Transaction> importedTransactions);
    DateTime GetLatestTransactionDate(List<Transaction> transactions);
}

public class CsvFileArchiveService : ICsvFileArchiveService
{
    public async Task<bool> ArchiveFileAsync(
        string sourceFilePath,
        string? processedFolderPath,
        List<Transaction> importedTransactions)
    {
        // Return immediately to make async
        await Task.CompletedTask;

        // Validate inputs
        if (string.IsNullOrEmpty(sourceFilePath))
        {
            Console.WriteLine("⚠ Warning: Cannot archive - source file path is null or empty");
            return false;
        }

        if (string.IsNullOrEmpty(processedFolderPath))
        {
            Console.WriteLine($"⚠ Warning: Processed folder not configured for {Path.GetFileName(sourceFilePath)}");
            return false;
        }

        if (!File.Exists(sourceFilePath))
        {
            Console.WriteLine($"⚠ Warning: Source file not found: {sourceFilePath}");
            return false;
        }

        try
        {
            // Ensure processed folder exists
            if (!Directory.Exists(processedFolderPath))
            {
                Directory.CreateDirectory(processedFolderPath);
            }

            // Get latest transaction date
            var latestDate = GetLatestTransactionDate(importedTransactions);

            // Determine transaction type from the first transaction in the list
            var transactionTypeName = GetTransactionTypeName(importedTransactions);

            // Construct clean filename: {transactiontype}_{YYYYMMDD}_{HHmmss}.csv
            var extension = Path.GetExtension(sourceFilePath);
            var dateString = latestDate.ToString("yyyyMMdd");
            var timeString = DateTime.Now.ToString("HHmmss");
            var newFileName = $"{transactionTypeName}_{dateString}_{timeString}{extension}";

            // Construct destination path
            var destinationPath = Path.Combine(processedFolderPath, newFileName);

            // If file still exists (rare edge case within same second), append a counter
            var counter = 1;
            while (File.Exists(destinationPath) && counter < 100)
            {
                newFileName = $"{transactionTypeName}_{dateString}_{timeString}_{counter:D2}{extension}";
                destinationPath = Path.Combine(processedFolderPath, newFileName);
                counter++;
            }

            if (File.Exists(destinationPath))
            {
                Console.WriteLine($"⚠ Warning: Could not generate unique filename after 100 attempts: {newFileName}");
                return false;
            }

            // Move file to processed folder
            File.Move(sourceFilePath, destinationPath, overwrite: false);

            return true;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"⚠ Error: Failed to archive {Path.GetFileName(sourceFilePath)}: {ex.Message}");
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"⚠ Error: Permission denied while archiving {Path.GetFileName(sourceFilePath)}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Error: Unexpected error archiving {Path.GetFileName(sourceFilePath)}: {ex.Message}");
            return false;
        }
    }

    public DateTime GetLatestTransactionDate(List<Transaction> transactions)
    {
        if (transactions == null || transactions.Count == 0)
        {
            // Return today's date as fallback if no transactions
            return DateTime.Today;
        }

        try
        {
            // Get the maximum date from all transactions
            // Works for all transaction types (RBC, Amex, PCFinancial) as they all have the Date property
            return transactions.Max(t => t.Date);
        }
        catch (Exception)
        {
            // Fallback to today if any issues
            return DateTime.Today;
        }
    }

    private string GetTransactionTypeName(List<Transaction> transactions)
    {
        if (transactions == null || transactions.Count == 0)
        {
            return "unknown";
        }

        // Determine type from first transaction
        var firstTransaction = transactions.First();

        if (firstTransaction is RBCTransaction)
            return "rbc";
        else if (firstTransaction is AmexTransaction)
            return "amex";
        else if (firstTransaction is PCFinancialTransaction)
            return "pcfinancial";
        else
            return "unknown";
    }
}
