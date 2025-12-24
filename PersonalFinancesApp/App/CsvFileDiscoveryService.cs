using PersonalFinances.Models;

namespace PersonalFinances.App;

public interface ICsvFileDiscoveryService
{
    Task EnsureFoldersExistAsync(CsvImportSettings? settings);
    Task<Dictionary<string, Type>> DiscoverCsvFilesAsync(CsvImportSettings? settings);
    Task<bool> HasUnprocessedFilesAsync(CsvImportSettings? settings);
}

public class CsvFileDiscoveryService : ICsvFileDiscoveryService
{
    public async Task EnsureFoldersExistAsync(CsvImportSettings? settings)
    {
        await Task.CompletedTask;

        if (settings == null)
        {
            return;
        }

        // Create RBC folders
        CreateFoldersForType(settings.RBC, "RBC");

        // Create Amex folders
        CreateFoldersForType(settings.Amex, "Amex");

        // Create PC Financial folders
        CreateFoldersForType(settings.PCFinancial, "PC Financial");
    }

    private void CreateFoldersForType(CsvTransactionTypeSettings? typeSettings, string typeName)
    {
        if (typeSettings == null)
        {
            return;
        }

        try
        {
            // Create Input folder
            if (!string.IsNullOrEmpty(typeSettings.InputFolder) && !Directory.Exists(typeSettings.InputFolder))
            {
                Directory.CreateDirectory(typeSettings.InputFolder);
                Console.WriteLine($"✓ Created {typeName} input folder: {typeSettings.InputFolder}");
            }

            // Create Processed folder
            if (!string.IsNullOrEmpty(typeSettings.ProcessedFolder) && !Directory.Exists(typeSettings.ProcessedFolder))
            {
                Directory.CreateDirectory(typeSettings.ProcessedFolder);
                Console.WriteLine($"✓ Created {typeName} processed folder: {typeSettings.ProcessedFolder}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Warning: Could not create {typeName} folders: {ex.Message}");
        }
    }

    public async Task<Dictionary<string, Type>> DiscoverCsvFilesAsync(CsvImportSettings? settings)
    {
        var result = new Dictionary<string, Type>();

        if (settings == null)
        {
            return result;
        }

        // Discover RBC CSV files
        await DiscoverFilesForTypeAsync(
            settings.RBC?.InputFolder,
            typeof(RBCTransaction),
            "RBC",
            result);

        // Discover Amex CSV files
        await DiscoverFilesForTypeAsync(
            settings.Amex?.InputFolder,
            typeof(AmexTransaction),
            "Amex",
            result);

        // Discover PC Financial CSV files
        await DiscoverFilesForTypeAsync(
            settings.PCFinancial?.InputFolder,
            typeof(PCFinancialTransaction),
            "PC Financial",
            result);

        return result;
    }

    public async Task<bool> HasUnprocessedFilesAsync(CsvImportSettings? settings)
    {
        var discoveredFiles = await DiscoverCsvFilesAsync(settings);
        return discoveredFiles.Count > 0;
    }

    private async Task DiscoverFilesForTypeAsync(
        string? inputFolder,
        Type transactionType,
        string typeName,
        Dictionary<string, Type> result)
    {
        // Return immediately to make async
        await Task.CompletedTask;

        if (string.IsNullOrEmpty(inputFolder))
        {
            return; // Folder not configured, skip
        }

        try
        {
            if (!Directory.Exists(inputFolder))
            {
                Console.WriteLine($"⚠ Input folder not found for {typeName}: {inputFolder}");
                return;
            }

            var csvFiles = Directory.EnumerateFiles(inputFolder, "*.csv", SearchOption.TopDirectoryOnly);
            var fileCount = 0;

            foreach (var filePath in csvFiles)
            {
                // Skip if file already discovered (edge case: overlapping folders)
                if (!result.ContainsKey(filePath))
                {
                    result[filePath] = transactionType;
                    fileCount++;
                }
            }

            if (fileCount == 0)
            {
                Console.WriteLine($"ℹ No CSV files found in {inputFolder}");
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"⚠ Warning: Could not access {typeName} folder '{inputFolder}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"⚠ Warning: Permission denied for {typeName} folder '{inputFolder}': {ex.Message}");
        }
    }
}
