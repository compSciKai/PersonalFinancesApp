using PersonalFinances.Extentions;
public class BudgetProfile
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, double> BudgetCategories { get; set; }

    public BudgetProfile(string name, Dictionary<string, double> budgetCategories, string description="")
    {
        Name = name;
        Description = description;
        BudgetCategories = budgetCategories;
    }

    public override string ToString()
    {
        return $"Name: {Name}\nDescription: {Description}\n\nBudget:\n{BudgetCategories.ToString(": $", "\n")}";
    }
}