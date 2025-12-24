using PersonalFinances.Models;

namespace PersonalFinances.App;

public interface ICsvImportOrchestrator
{
    Task<Dictionary<string, Type>?> GetTransactionsToProcessAsync(
        Dictionary<string, Type>? transactionsDictionary,
        CsvImportSettings? csvSettings,
        ITransactionsUserInteraction userInteraction);
}

public class CsvImportOrchestrator : ICsvImportOrchestrator
{
    private readonly ICsvFileDiscoveryService _csvFileDiscoveryService;

    public CsvImportOrchestrator(ICsvFileDiscoveryService csvFileDiscoveryService)
    {
        _csvFileDiscoveryService = csvFileDiscoveryService;
    }

    public async Task<Dictionary<string, Type>?> GetTransactionsToProcessAsync(
        Dictionary<string, Type>? transactionsDictionary,
        CsvImportSettings? csvSettings,
        ITransactionsUserInteraction userInteraction)
    {
        // Priority 1: Use hardcoded dictionary if provided and not empty (backward compatibility)
        if (transactionsDictionary != null && transactionsDictionary.Count > 0)
        {
            Console.WriteLine("Using hardcoded transaction dictionary (backward compatibility mode)");
            return transactionsDictionary;
        }

        // Ensure configured folders exist before scanning
        await _csvFileDiscoveryService.EnsureFoldersExistAsync(csvSettings);

        // Priority 2: Scan configured folders for CSV files
        Console.WriteLine("Scanning configured folders for CSV files...");
        var discoveredFiles = await _csvFileDiscoveryService.DiscoverCsvFilesAsync(csvSettings);

        if (discoveredFiles.Count == 0)
        {
            Console.WriteLine("No unprocessed CSV files found.");
            return new Dictionary<string, Type>(); // Return empty dictionary
        }

        // Files found - prompt user
        Console.WriteLine($"\nFound {discoveredFiles.Count} unprocessed CSV file(s):");
        foreach (var file in discoveredFiles)
        {
            var fileName = Path.GetFileName(file.Key);
            var typeName = GetTransactionTypeName(file.Value);
            Console.WriteLine($"  - {fileName} ({typeName})");
        }

        // Prompt user for confirmation
        while (true)
        {
            Console.WriteLine("\nProcess now? (Y/N): ");
            var input = userInteraction.GetInput()?.Trim().ToLower();

            if (input == "y" || input == "yes")
            {
                return discoveredFiles;
            }
            else if (input == "n" || input == "no")
            {
                Console.WriteLine("Skipping CSV import.");
                return new Dictionary<string, Type>(); // Return empty dictionary
            }
            else
            {
                Console.WriteLine("Invalid choice. Please enter Y or N.");
            }
        }
    }

    private string GetTransactionTypeName(Type transactionType)
    {
        if (transactionType == typeof(RBCTransaction))
            return "RBC";
        else if (transactionType == typeof(AmexTransaction))
            return "Amex";
        else if (transactionType == typeof(PCFinancialTransaction))
            return "PC Financial";
        else
            return transactionType.Name;
    }
}
