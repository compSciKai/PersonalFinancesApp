namespace PersonalFinances.Models;

/// <summary>
/// Configuration for automated CSV fetching from bank websites
/// </summary>
public class AutomatedFetchSettings
{
    /// <summary>
    /// Whether automated fetching is enabled for this vendor
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Initial URL to navigate to (typically the login page)
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Sequential automation steps to execute
    /// </summary>
    public List<AutomationStep>? Steps { get; set; }

    /// <summary>
    /// Whether to output verbose step-by-step progress logging
    /// Defaults to true for backward compatibility
    /// </summary>
    public bool VerboseLogging { get; set; } = true;
}

/// <summary>
/// Represents a single automation step in the browser automation workflow
/// </summary>
public class AutomationStep
{
    /// <summary>
    /// Type of automation step: Navigate, WaitForUserAuth, WaitForUrl, Click, WaitForElement, WaitForDownload
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// URL to navigate to (for Navigate and WaitForUrl steps)
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Message to display to user (for WaitForUserAuth step)
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Element selector (for Click and WaitForElement steps)
    /// </summary>
    public string? Selector { get; set; }

    /// <summary>
    /// Type of selector: LinkText, PartialLinkText, ButtonText, CssSelector, XPath, Id, Name, ClassName
    /// </summary>
    public string? SelectorType { get; set; }

    /// <summary>
    /// Timeout in seconds for this step
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
