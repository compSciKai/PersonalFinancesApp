namespace PersonalFinances.App;

using PersonalFinances.Models;
using System;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Service for orchestrating automated CSV fetching from multiple vendors
/// </summary>
public class CsvFetchService : ICsvFetchService
{
    private readonly IBrowserAutomationService _browserAutomationService;
    private readonly CsvImportSettings _csvImportSettings;

    public CsvFetchService(
        IBrowserAutomationService browserAutomationService,
        CsvImportSettings csvImportSettings)
    {
        _browserAutomationService = browserAutomationService;
        _csvImportSettings = csvImportSettings;
    }

    public async Task<CsvFetchResult> FetchTransactionsForVendorAsync(string vendorName)
    {
        // Get vendor configuration
        var vendorSettings = GetVendorSettings(vendorName);
        if (vendorSettings == null)
        {
            return new CsvFetchResult
            {
                Success = false,
                ErrorMessage = $"No configuration found for vendor: {vendorName}"
            };
        }

        // Check if automated fetch is enabled
        if (vendorSettings.AutomatedFetch == null || !vendorSettings.AutomatedFetch.Enabled)
        {
            return new CsvFetchResult
            {
                Success = false,
                ErrorMessage = $"Automated fetch is not enabled for {vendorName}"
            };
        }

        // Ensure input folder exists
        var inputFolder = vendorSettings.InputFolder;
        if (string.IsNullOrEmpty(inputFolder))
        {
            return new CsvFetchResult
            {
                Success = false,
                ErrorMessage = $"InputFolder not configured for {vendorName}"
            };
        }

        try
        {
            // Create input folder if it doesn't exist
            if (!Directory.Exists(inputFolder))
            {
                Directory.CreateDirectory(inputFolder);
            }

            // Execute browser automation
            var result = await _browserAutomationService.ExecuteAutomationAsync(
                vendorSettings.AutomatedFetch,
                inputFolder);

            if (result.Success)
            {
                // Rename downloaded files with vendor-specific names and timestamps
                var renamedFiles = RenameDownloadedFiles(vendorName, inputFolder, result.DownloadedFilePaths);

                return new CsvFetchResult
                {
                    Success = true,
                    FilesDownloaded = renamedFiles.Count,
                    DownloadedFilePath = renamedFiles.FirstOrDefault()
                };
            }
            else
            {
                return new CsvFetchResult
                {
                    Success = false,
                    ErrorMessage = result.ErrorMessage
                };
            }
        }
        catch (Exception ex)
        {
            return new CsvFetchResult
            {
                Success = false,
                ErrorMessage = $"Error fetching transactions for {vendorName}: {ex.Message}"
            };
        }
    }

    private List<string> RenameDownloadedFiles(string vendorName, string inputFolder, List<string> downloadedFilePaths)
    {
        var renamedFiles = new List<string>();
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        for (int i = 0; i < downloadedFilePaths.Count; i++)
        {
            var originalPath = downloadedFilePaths[i];
            if (!File.Exists(originalPath))
            {
                Console.WriteLine($"⚠️  Warning: Downloaded file not found: {originalPath}");
                continue;
            }

            string newFileName;
            var originalFileName = Path.GetFileName(originalPath);

            // Generate vendor-specific filename
            if (vendorName.ToUpperInvariant() == "AMEX")
            {
                // For Amex, distinguish between latest transactions and recent statement
                if (i == 0)
                {
                    newFileName = $"amex_latest_transactions_{timestamp}.csv";
                }
                else if (i == 1)
                {
                    newFileName = $"amex_recent_statement_{timestamp}.csv";
                }
                else
                {
                    newFileName = $"amex_download_{i + 1}_{timestamp}.csv";
                }
            }
            else
            {
                // For other vendors, use simple naming with counter if multiple files
                var fileCounter = downloadedFilePaths.Count > 1 ? $"_{i + 1}" : "";
                newFileName = $"{vendorName.ToLower()}_transactions{fileCounter}_{timestamp}.csv";
            }

            var newPath = Path.Combine(inputFolder, newFileName);

            try
            {
                File.Move(originalPath, newPath);
                renamedFiles.Add(newPath);
                Console.WriteLine($"✅ Renamed: {originalFileName} → {newFileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Warning: Failed to rename {originalFileName}: {ex.Message}");
                renamedFiles.Add(originalPath); // Keep original path if rename fails
            }
        }

        return renamedFiles;
    }

    private CsvTransactionTypeSettings? GetVendorSettings(string vendorName)
    {
        return vendorName.ToUpperInvariant() switch
        {
            "RBC" => _csvImportSettings.RBC,
            "AMEX" => _csvImportSettings.Amex,
            "PCFINANCIAL" => _csvImportSettings.PCFinancial,
            "PC FINANCIAL" => _csvImportSettings.PCFinancial,
            _ => null
        };
    }
}
