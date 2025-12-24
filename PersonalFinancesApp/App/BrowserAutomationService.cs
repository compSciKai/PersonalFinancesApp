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

        try
        {
            // Initialize Chrome with download folder configuration
            InitializeDriver(downloadFolderPath);

            // Execute steps sequentially
            foreach (var step in settings.Steps)
            {
                var result = await ExecuteStepAsync(step, downloadFolderPath);
                if (!result.Success)
                {
                    // Keep browser open for inspection on error
                    Console.WriteLine("\n⚠️  Browser window left open for inspection.");
                    Console.WriteLine("    Press Enter when you're done inspecting, and the browser will close...");
                    Console.ReadLine();
                    Cleanup();
                    return result;
                }

                // If step was WaitForDownload, return the downloaded file path
                if (step.Type == "WaitForDownload" && !string.IsNullOrEmpty(result.DownloadedFilePath))
                {
                    Cleanup();
                    return result;
                }
            }

            // Success - cleanup and return
            Cleanup();
            return new BrowserAutomationResult
            {
                Success = true,
                ErrorMessage = null
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

        // Keep browser visible for user authentication
        // options.AddArgument("--headless"); // NOT using headless mode

        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
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
            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = $"Timeout during {step.Type} step: {ex.Message}"
            };
        }
        catch (NoSuchElementException ex)
        {
            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = $"Element not found during {step.Type} step: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new BrowserAutomationResult
            {
                Success = false,
                ErrorMessage = $"Error during {step.Type} step: {ex.Message}"
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
                    DownloadedFilePath = candidateFiles.First()
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
            "ButtonText" => By.XPath($"//button[text()='{step.Selector}']"),
            "CssSelector" => By.CssSelector(step.Selector!),
            "XPath" => By.XPath(step.Selector!),
            "Id" => By.Id(step.Selector!),
            "Name" => By.Name(step.Selector!),
            "ClassName" => By.ClassName(step.Selector!),
            _ => throw new ArgumentException($"Unknown selector type: {step.SelectorType}")
        };
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
