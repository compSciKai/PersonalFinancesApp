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
                return new CsvFetchResult
                {
                    Success = true,
                    FilesDownloaded = string.IsNullOrEmpty(result.DownloadedFilePath) ? 0 : 1,
                    DownloadedFilePath = result.DownloadedFilePath
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
