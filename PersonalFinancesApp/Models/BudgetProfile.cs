using PersonalFinances.Extentions;
public class BudgetProfile
{
    public string Name { get; set; }
    public string Description { get; set; }
    public double Income { get; set; }
    public Dictionary<string, double> BudgetCategories { get; set; }

    public BudgetProfile(string name, Dictionary<string, double> budgetCategories, double income, string description="")
    {
        Name = name;
        Description = description;
        BudgetCategories = budgetCategories;
        Income = income;
    }

    public override string ToString()
    {
        return $"Name: {Name}\nDescription: {Description}\nIncome: ${Income.ToString("0.00")}\n\nBudget:\n{BudgetCategories.ToString(": $", "\n")}";
    }
}