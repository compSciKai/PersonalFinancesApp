namespace PersonalFinances.App;

using PersonalFinances.Models;

/// <summary>
/// Service for executing browser automation steps using Selenium WebDriver
/// </summary>
public interface IBrowserAutomationService
{
    /// <summary>
    /// Executes the automated fetch workflow for a vendor
    /// </summary>
    /// <param name="settings">Automation settings with steps to execute</param>
    /// <param name="downloadFolderPath">Absolute path to download folder</param>
    /// <returns>Result indicating success/failure and downloaded file path</returns>
    Task<BrowserAutomationResult> ExecuteAutomationAsync(
        AutomatedFetchSettings settings,
        string downloadFolderPath);
}

/// <summary>
/// Result of browser automation execution
/// </summary>
public class BrowserAutomationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// List of all files downloaded during the automation session
    /// </summary>
    public List<string> DownloadedFilePaths { get; set; } = new List<string>();
}
