namespace PersonalFinances.Models;

/// <summary>
/// Configuration settings for transfer management matching logic
/// </summary>
public class TransferManagementSettings
{
    /// <summary>
    /// Minimum confidence score required to show a transfer match to the user.
    /// Values: 3 = Low+, 4 = Medium+, 5 = High only
    /// Default: 4 (Medium+ confidence)
    /// </summary>
    public int MinimumConfidenceThreshold { get; set; } = 4;
}
