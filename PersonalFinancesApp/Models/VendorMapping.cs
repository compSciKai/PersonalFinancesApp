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
}
