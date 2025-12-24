namespace PersonalFinances.App;

/// <summary>
/// High-level service for fetching CSV transactions from bank websites
/// </summary>
public interface ICsvFetchService
{
    /// <summary>
    /// Fetches transactions for a specific vendor using automated browser automation
    /// </summary>
    /// <param name="vendorName">Vendor name (RBC, Amex, PCFinancial)</param>
    /// <returns>Result indicating success/failure and number of files downloaded</returns>
    Task<CsvFetchResult> FetchTransactionsForVendorAsync(string vendorName);
}

/// <summary>
/// Result of CSV fetch operation
/// </summary>
public class CsvFetchResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int FilesDownloaded { get; set; }
    public string? DownloadedFilePath { get; set; }
}
