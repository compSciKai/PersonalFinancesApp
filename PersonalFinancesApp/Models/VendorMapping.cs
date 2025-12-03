using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PersonalFinances.Models;

public class Category : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    // When true, transactions in this category are tracked but don't count against budget
    // Examples: mortgage, credit card payments, account transfers
    public bool IsTrackedOnly { get; set; } = false;

    // Navigation property
    public virtual ICollection<VendorMapping> Vendors { get; set; } = new List<VendorMapping>();
}

public class VendorMapping : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Pattern { get; set; } = string.Empty;  // e.g., "starbucks", "microsoft*console"

    [Required]
    [StringLength(200)]
    public string VendorName { get; set; } = string.Empty;

    // Foreign key to Category (nullable since not all vendors have categories initially)
    public int? CategoryId { get; set; }

    // Navigation property
    [JsonIgnore]
    public virtual Category? Category { get; set; }

    // Transaction type hint for auto-detection
    public TransactionType? SuggestedType { get; set; }

    // When true, all transactions matching this vendor will be forced to SuggestedType
    // When false, SuggestedType is just a hint and can be overridden by detection rules
    public bool OverrideType { get; set; } = false;
}
