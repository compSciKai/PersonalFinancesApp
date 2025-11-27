namespace PersonalFinances.Utilities;

/// <summary>
/// Utility for formatting text within ASCII box borders with proper alignment.
/// </summary>
public static class BoxFormatter
{
    private const int DefaultBoxWidth = 71; // Total width including borders
    private const int ContentWidth = 69; // Width between the borders (71 - 2)

    /// <summary>
    /// Formats a line of text to fit within a box with proper padding.
    /// </summary>
    /// <param name="content">The text content to display</param>
    /// <param name="boxWidth">Total box width (default: 71)</param>
    /// <returns>Formatted line with borders and proper padding</returns>
    public static string FormatLine(string content, int boxWidth = DefaultBoxWidth)
    {
        int contentWidth = boxWidth - 2; // Subtract 2 for the left and right borders

        if (content.Length > contentWidth)
        {
            // Truncate if too long
            content = content.Substring(0, contentWidth - 3) + "...";
        }

        // Pad to exact width
        string paddedContent = content.PadRight(contentWidth);

        return $"║ {paddedContent} ║";
    }

    /// <summary>
    /// Creates a full error box with proper formatting.
    /// </summary>
    public static string CreateErrorBox(string title, string errorMessage, params string[] additionalLines)
    {
        var lines = new List<string>();

        // Top border
        lines.Add("╔═══════════════════════════════════════════════════════════════════════╗");

        // Title (centered)
        lines.Add(FormatLine(CenterText(title)));

        // Separator
        lines.Add("╠═══════════════════════════════════════════════════════════════════════╣");

        // Error message
        lines.Add(FormatLine(errorMessage));

        // Empty line if there are additional lines
        if (additionalLines.Length > 0)
        {
            lines.Add(FormatLine(""));
        }

        // Additional lines
        foreach (var line in additionalLines)
        {
            lines.Add(FormatLine(line));
        }

        // Bottom border
        lines.Add("╚═══════════════════════════════════════════════════════════════════════╝");

        return "\n" + string.Join("\n", lines) + "\n";
    }

    /// <summary>
    /// Centers text within the content width.
    /// </summary>
    private static string CenterText(string text)
    {
        if (text.Length >= ContentWidth)
        {
            return text.Substring(0, ContentWidth);
        }

        int totalPadding = ContentWidth - text.Length;
        int leftPadding = totalPadding / 2;
        int rightPadding = totalPadding - leftPadding;

        return new string(' ', leftPadding) + text + new string(' ', rightPadding);
    }
}
