using PersonalFinances.Extentions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace PersonalFinances.Models;

public class BudgetProfile : BaseEntity
{
    public string Name { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Description { get; set; }
    public double Income { get; set; }

    // Navigation property for database relationship
    public virtual ICollection<BudgetCategory> Categories { get; set; } = new List<BudgetCategory>();

    // Dictionary for backward compatibility with JSON storage - not mapped to database
    [NotMapped]
    [JsonPropertyName("BudgetCategories")]
    public Dictionary<string, double> BudgetCategories
    {
        get => Categories?.ToDictionary(c => c.CategoryName, c => c.BudgetAmount) ?? new Dictionary<string, double>();
        set
        {
            Categories = value?.Select(kvp => new BudgetCategory
            {
                CategoryName = kvp.Key,
                BudgetAmount = kvp.Value
            }).ToList() ?? new List<BudgetCategory>();
        }
    }

    // Parameterless constructor for EF Core
    public BudgetProfile() { }

    public BudgetProfile(string name, Dictionary<string, double> budgetCategories, double income, string username, string description="")
    {
        Name = name;
        Description = description;
        BudgetCategories = budgetCategories;
        Income = income;
        UserName = username;
    }

    /// <summary>
    /// Validates that there are no duplicate category names in the profile.
    /// Case-insensitive comparison.
    /// </summary>
    /// <returns>Tuple with IsValid flag and list of duplicate category names</returns>
    public (bool IsValid, List<string> DuplicateCategories) ValidateCategories()
    {
        var duplicates = Categories
            .GroupBy(c => c.CategoryName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        return (!duplicates.Any(), duplicates);
    }

    public override string ToString()
    {
        return $"Name: {Name}\nDescription: {Description}\nIncome: ${Income.ToString("0.00")}\n\nBudget:\n{BudgetCategories.ToString(": $", "\n")}";
    }
}

public class BudgetCategory : BaseEntity
{
    [Required]
    public int BudgetProfileId { get; set; }

    [Required]
    [StringLength(100)]
    public string CategoryName { get; set; }

    [Required]
    public double BudgetAmount { get; set; }

    // Navigation property back to BudgetProfile
    [JsonIgnore]
    public virtual BudgetProfile BudgetProfile { get; set; }

    public BudgetCategory() { }

    public BudgetCategory(string categoryName, double budgetAmount)
    {
        CategoryName = categoryName;
        BudgetAmount = budgetAmount;
    }
}