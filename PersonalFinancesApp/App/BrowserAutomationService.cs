namespace PersonalFinances.App;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using PersonalFinances.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Selenium WebDriver-based browser automation service
/// </summary>
public class BrowserAutomationService : IBrowserAutomationService, IDisposable
{
    private IWebDriver? _driver;
    private bool _disposed;

    public async Task<BrowserAutomationResult> ExecuteAutomationAsync(
        AutomatedFetchSettings settings,
        string downloadFolderPath)
    {
        if (settings?.Steps == null || !settings.Steps.Any())
        {
            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = "No automation steps configured"
            };
        }

        var downloadedFiles = new List<string>();

        try
        {
            // Initialize Chrome with download folder configuration
            InitializeDriver(downloadFolderPath);

            if (settings.VerboseLogging)
            {
                Console.WriteLine($"\n🤖 Starting automation: {settings.Steps.Count} steps");
                Console.WriteLine($"📁 Download folder: {downloadFolderPath}\n");
            }

            // Execute steps sequentially
            int stepNumber = 0;
            foreach (var step in settings.Steps)
            {
                stepNumber++;

                if (settings.VerboseLogging)
                {
                    Console.WriteLine($"[{stepNumber}/{settings.Steps.Count}] ⏳ {GetStepDescription(step)}");
                }

                var result = await ExecuteStepAsync(step, downloadFolderPath);
                if (!result.Success)
                {
                    // ALWAYS show errors regardless of VerboseLogging
                    Console.WriteLine($"[{stepNumber}/{settings.Steps.Count}] ❌ FAILED");

                    // Capture screenshot on failure
                    CaptureFailureScreenshot(stepNumber);

                    // Keep browser open for inspection on error
                    Console.WriteLine("\n⚠️  Browser window left open for inspection.");
                    Console.WriteLine("    Press Enter when you're done inspecting, and the browser will close...");
                    Console.ReadLine();
                    Cleanup();
                    return result;
                }

                if (settings.VerboseLogging)
                {
                    Console.WriteLine($"[{stepNumber}/{settings.Steps.Count}] ✅ Success");
                }

                // If step was WaitForDownload, collect the downloaded file path
                if (step.Type == "WaitForDownload" && result.DownloadedFilePaths.Any())
                {
                    downloadedFiles.AddRange(result.DownloadedFilePaths);
                }
            }

            // Success - cleanup and return all downloaded files
            Cleanup();

            if (settings.VerboseLogging)
            {
                Console.WriteLine($"\n✅ Automation completed successfully!");
                Console.WriteLine($"📥 Downloaded {downloadedFiles.Count} file(s)");
            }

            return new BrowserAutomationResult
            {
                Success = true,
                ErrorMessage = null,
                DownloadedFilePaths = downloadedFiles
            };
        }
        catch (Exception ex)
        {
            // Keep browser open for inspection on unexpected errors
            Console.WriteLine("\n⚠️  Unexpected error occurred. Browser window left open for inspection.");
            Console.WriteLine("    Press Enter when you're done inspecting, and the browser will close...");
            Console.ReadLine();
            Cleanup();
            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = $"Automation failed: {ex.Message}"
            };
        }
    }

    private void InitializeDriver(string downloadFolderPath)
    {
        var options = new ChromeOptions();

        // Configure download behavior
        options.AddUserProfilePreference("download.default_directory", Path.GetFullPath(downloadFolderPath));
        options.AddUserProfilePreference("download.prompt_for_download", false);
        options.AddUserProfilePreference("download.directory_upgrade", true);
        options.AddUserProfilePreference("safebrowsing.enabled", true);

        // Anti-bot detection: Make Chrome appear like a normal browser
        options.AddExcludedArgument("enable-automation");  // Remove "Chrome is being controlled" banner
        options.AddArgument("--disable-blink-features=AutomationControlled");  // Hide automation flag
        options.AddAdditionalOption("useAutomationExtension", false);  // Disable automation extension

        // Additional flags to appear more like a real browser
        options.AddArgument("--disable-infobars");  // Remove info bars
        options.AddArgument("--start-maximized");  // Start maximized like normal usage

        // Keep browser visible for user authentication
        // options.AddArgument("--headless"); // NOT using headless mode

        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

        // Execute JavaScript to remove navigator.webdriver flag (strongest detection method)
        ((IJavaScriptExecutor)_driver).ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
    }

    private async Task<BrowserAutomationResult> ExecuteStepAsync(AutomationStep step, string downloadFolderPath)
    {
        if (_driver == null)
        {
            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = "WebDriver not initialized"
            };
        }

        try
        {
            switch (step.Type)
            {
                case "Navigate":
                    return ExecuteNavigate(step);

                case "WaitForUserAuth":
                    return ExecuteWaitForUserAuth(step);

                case "WaitForUrl":
                    return ExecuteWaitForUrl(step);

                case "Click":
                    return ExecuteClick(step);

                case "ClickLabel":
                    return ExecuteClickLabel(step);

                case "CheckCheckbox":
                    return ExecuteCheckCheckbox(step);

                case "WaitForElement":
                    return ExecuteWaitForElement(step);

                case "WaitForDownload":
                    return await ExecuteWaitForDownloadAsync(step, downloadFolderPath);

                default:
                    return new BrowserAutomationResult
                    {
                        Success = false,
                        ErrorMessage = $"Unknown step type: {step.Type}"
                    };
            }
        }
        catch (WebDriverTimeoutException ex)
        {
            var currentUrl = _driver?.Url ?? "unknown";

            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = $"Timeout during {step.Type} step ({step.TimeoutSeconds}s)\n" +
                              $"  Selector: {step.Selector} ({step.SelectorType})\n" +
                              $"  Current URL: {currentUrl}\n" +
                              $"  Details: {ex.Message}"
            };
        }
        catch (NoSuchElementException ex)
        {
            var currentUrl = _driver?.Url ?? "unknown";
            var pageTitle = _driver?.Title ?? "unknown";

            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = $"Element not found during {step.Type} step\n" +
                              $"  Selector: {step.Selector} ({step.SelectorType})\n" +
                              $"  Current URL: {currentUrl}\n" +
                              $"  Page Title: {pageTitle}\n" +
                              $"  Details: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            var currentUrl = _driver?.Url ?? "unknown";

            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = $"Error during {step.Type} step\n" +
                              $"  Current URL: {currentUrl}\n" +
                              $"  Details: {ex.Message}"
            };
        }
    }

    private BrowserAutomationResult ExecuteNavigate(AutomationStep step)
    {
        if (string.IsNullOrEmpty(step.Url))
        {
            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = "Navigate step requires Url"
            };
        }

        _driver!.Navigate().GoToUrl(step.Url);

        return new BrowserAutomationResult { Success = true };
    }

    private BrowserAutomationResult ExecuteWaitForUserAuth(AutomationStep step)
    {
        Console.WriteLine($"\n{step.Message ?? "Please complete authentication and press Enter..."}");
        Console.ReadLine();

        return new BrowserAutomationResult { Success = true };
    }

    private BrowserAutomationResult ExecuteWaitForUrl(AutomationStep step)
    {
        if (string.IsNullOrEmpty(step.Url))
        {
            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = "WaitForUrl step requires Url"
            };
        }

        var wait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(step.TimeoutSeconds));
        wait.Until(d => d.Url.Contains(step.Url));

        return new BrowserAutomationResult { Success = true };
    }

    private BrowserAutomationResult ExecuteClick(AutomationStep step)
    {
        if (string.IsNullOrEmpty(step.Selector))
        {
            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = "Click step requires Selector"
            };
        }

        var element = FindElement(step);
        element.Click();

        // Small delay after click to allow page navigation
        Thread.Sleep(1000);

        return new BrowserAutomationResult { Success = true };
    }

    private BrowserAutomationResult ExecuteClickLabel(AutomationStep step)
    {
        if (string.IsNullOrEmpty(step.Selector))
        {
            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = "ClickLabel step requires Selector"
            };
        }

        // For clicking label text (used when clicking radio/checkbox inputs is intercepted)
        // Selector should be the label text content
        var by = By.XPath($"//label[contains(text(), '{step.Selector}')] | //div[text()='{step.Selector}']");
        var wait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(step.TimeoutSeconds));
        var element = wait.Until(d => d.FindElement(by));
        element.Click();

        // Small delay after click
        Thread.Sleep(500);

        return new BrowserAutomationResult { Success = true };
    }

    private BrowserAutomationResult ExecuteCheckCheckbox(AutomationStep step)
    {
        if (string.IsNullOrEmpty(step.Selector))
        {
            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = "CheckCheckbox step requires Selector"
            };
        }

        // Find the checkbox label by text
        var labelBy = By.XPath($"//label[contains(text(), '{step.Selector}')]");
        var wait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(step.TimeoutSeconds));
        var label = wait.Until(d => d.FindElement(labelBy));

        // Find associated checkbox input (typically via 'for' attribute or parent/sibling relationship)
        IWebElement? checkbox = null;
        try
        {
            // Try to find checkbox via label's 'for' attribute
            var forAttr = label.GetAttribute("for");
            if (!string.IsNullOrEmpty(forAttr))
            {
                checkbox = _driver!.FindElement(By.Id(forAttr));
            }
        }
        catch
        {
            // If not found, try to find checkbox as sibling or child
            try
            {
                checkbox = label.FindElement(By.XPath(".//input[@type='checkbox'] | ./preceding-sibling::input[@type='checkbox'] | ./following-sibling::input[@type='checkbox']"));
            }
            catch
            {
                // Fallback: just click the label regardless of current state
                label.Click();
                Thread.Sleep(500);
                return new BrowserAutomationResult { Success = true };
            }
        }

        // Check if checkbox is already checked
        if (checkbox != null && !checkbox.Selected)
        {
            // Click the label to check it
            label.Click();
            Thread.Sleep(500);
        }

        return new BrowserAutomationResult { Success = true };
    }

    private BrowserAutomationResult ExecuteWaitForElement(AutomationStep step)
    {
        if (string.IsNullOrEmpty(step.Selector))
        {
            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = "WaitForElement step requires Selector"
            };
        }

        var wait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(step.TimeoutSeconds));
        wait.Until(d => FindElement(step) != null);

        return new BrowserAutomationResult { Success = true };
    }

    private async Task<BrowserAutomationResult> ExecuteWaitForDownloadAsync(AutomationStep step, string downloadFolderPath)
    {
        var absolutePath = Path.GetFullPath(downloadFolderPath);
        var timeout = TimeSpan.FromSeconds(step.TimeoutSeconds);
        var startTime = DateTime.Now;

        // Track files before download
        var filesBefore = Directory.Exists(absolutePath)
            ? Directory.GetFiles(absolutePath, "*.csv").ToHashSet()
            : new HashSet<string>();

        while (DateTime.Now - startTime < timeout)
        {
            await Task.Delay(500);

            if (!Directory.Exists(absolutePath))
                continue;

            var currentFiles = Directory.GetFiles(absolutePath, "*.csv");

            // Look for new CSV files (not in the before snapshot)
            var newFiles = currentFiles.Where(f => !filesBefore.Contains(f)).ToList();

            // Also look for recently modified files (handles instant downloads)
            var recentFiles = currentFiles
                .Where(f => File.GetLastWriteTime(f) >= startTime.AddSeconds(-5))
                .ToList();

            var candidateFiles = newFiles.Any() ? newFiles : recentFiles;

            if (candidateFiles.Any())
            {
                // Check if file is still downloading (.crdownload extension in Chrome)
                var partialDownloads = Directory.GetFiles(absolutePath, "*.crdownload");
                if (partialDownloads.Any())
                    continue;

                return new BrowserAutomationResult
                {
                    Success = true,
                    DownloadedFilePaths = new List<string> { candidateFiles.First() }
                };
            }
        }

        return new BrowserAutomationResult
        {
            Success = false,
            ErrorMessage = $"Download timeout: No CSV file appeared in {downloadFolderPath} within {step.TimeoutSeconds} seconds"
        };
    }

    private IWebElement FindElement(AutomationStep step)
    {
        var by = GetBySelector(step);
        var wait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(step.TimeoutSeconds));
        return wait.Until(d => d.FindElement(by));
    }

    private By GetBySelector(AutomationStep step)
    {
        return step.SelectorType switch
        {
            "LinkText" => By.LinkText(step.Selector!),
            "PartialLinkText" => By.PartialLinkText(step.Selector!),
            "ButtonText" => By.XPath($"//button[contains(., '{step.Selector}')]"),  // Use contains() to match nested text
            "CssSelector" => By.CssSelector(step.Selector!),
            "XPath" => By.XPath(step.Selector!),
            "Id" => By.Id(step.Selector!),
            "Name" => By.Name(step.Selector!),
            "ClassName" => By.ClassName(step.Selector!),
            _ => throw new ArgumentException($"Unknown selector type: {step.SelectorType}")
        };
    }

    /// <summary>
    /// Attempts to find element by data-testid, falling back to data-test-id if not found.
    /// Handles inconsistent attribute naming across different web applications.
    /// </summary>
    private IWebElement FindElementByTestId(string testId, int timeoutSeconds)
    {
        var wait = new WebDriverWait(_driver!, TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            // Try data-testid first (React Testing Library standard)
            return wait.Until(d => d.FindElement(By.CssSelector($"[data-testid='{testId}']")));
        }
        catch (WebDriverTimeoutException)
        {
            // Fallback to data-test-id (legacy/custom implementations)
            return wait.Until(d => d.FindElement(By.CssSelector($"[data-test-id='{testId}']")));
        }
    }

    /// <summary>
    /// Gets a human-readable description of an automation step for logging.
    /// </summary>
    private string GetStepDescription(AutomationStep step)
    {
        return step.Type switch
        {
            "Navigate" => $"Navigate to {step.Url}",
            "Click" => $"Click {step.Selector}",
            "ClickLabel" => $"Click label: {step.Selector}",
            "CheckCheckbox" => $"Check checkbox: {step.Selector}",
            "WaitForUserAuth" => "Wait for user authentication",
            "WaitForUrl" => $"Wait for URL: {step.Url}",
            "WaitForElement" => $"Wait for element: {step.Selector}",
            "WaitForDownload" => "Wait for download to complete",
            _ => $"{step.Type} step"
        };
    }

    /// <summary>
    /// Captures a screenshot on automation failure for debugging.
    /// Screenshot is saved to automation-logs/ directory.
    /// </summary>
    private void CaptureFailureScreenshot(int stepNumber)
    {
        try
        {
            var logsFolder = Path.Combine(Directory.GetCurrentDirectory(), "automation-logs");
            Directory.CreateDirectory(logsFolder);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var filename = $"{timestamp}-step-{stepNumber}-failure.png";
            var filepath = Path.Combine(logsFolder, filename);

            var screenshot = ((ITakesScreenshot)_driver!).GetScreenshot();
            screenshot.SaveAsFile(filepath);

            Console.WriteLine($"📸 Screenshot saved: {filepath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Failed to capture screenshot: {ex.Message}");
        }
    }

    private void Cleanup()
    {
        if (_driver != null)
        {
            try
            {
                _driver.Quit();
            }
            catch
            {
                // Ignore cleanup errors
            }
            finally
            {
                _driver.Dispose();
                _driver = null;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Cleanup();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
