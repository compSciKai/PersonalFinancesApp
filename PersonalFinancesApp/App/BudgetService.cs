using PersonalFinances.Models;
using PersonalFinances.Repositories;

namespace PersonalFinances.App;

public class BudgetService : IBudgetService
{
    Dictionary<string, BudgetProfile> BudgetProfiles {get; set;}
    IBudgetRepository _budgetRepository;
    ITransactionsUserInteraction _transactionUserInteraction;

    public BudgetService(
        IBudgetRepository budgetRepository,
        ITransactionsUserInteraction transactionUserInteraction)
    {
        _budgetRepository = budgetRepository;
        _transactionUserInteraction = transactionUserInteraction;
        BudgetProfiles = budgetRepository.LoadBudgetProfiles();
    }
    public BudgetProfile? GetProfile(string profileName)
    {
        BudgetProfile profile;
        if (BudgetProfiles.TryGetValue(profileName, out profile))
        {
            return profile;
        }

        return null;
    }

    public void StoreProfile(string profileName, BudgetProfile profile)
    {
        BudgetProfiles.Add(profileName, profile);
        _budgetRepository.SaveBudgetProfiles(BudgetProfiles);
    }

    List<string> GetProfileNames()
    {
        List<string> profileNames = new List<string>();

        foreach (var kvp in BudgetProfiles)
        {
            profileNames.Add(kvp.Key);
        }

        return profileNames;
    }

    public BudgetProfile? GetActiveProfile()
    {
                //TODO: set budget profile here
        /*
            consider asking user questinos
            which budget profile to use
            set categories here -> each profile with contain budget amounts for each category
            set date range

            budgetService
                - getProfile to return unique budget
                - setProfile to update budget categories
        */
        List<string> profileNames = GetProfileNames();
        string? profileChoice = null;
        if (profileNames.Count > 0)
        {
            profileChoice = _transactionUserInteraction.PromptForProfileChoice(profileNames);
        }

        BudgetProfile profile;
        if (profileChoice is not null)
        {
            return GetProfile(profileChoice);
        }

        return null;
    }

    public BudgetProfile CreateNewProfile()
    {
        _transactionUserInteraction.ShowMessage("What is the name for this profile?");
        string name = _transactionUserInteraction.GetInput();

        _transactionUserInteraction.ShowMessage("Set a description for this profile, or press enter.");
        string description = _transactionUserInteraction.GetInput();

        Dictionary<string, double> budgets = new Dictionary<string, double>();
        bool isMoreCategories = true;
        while (isMoreCategories)
        {
            _transactionUserInteraction.ShowMessage("Add a new budget category."
            + " Type 'f' when finished.");
            string newCategory = _transactionUserInteraction.GetInput();

            if (newCategory != "f")
            {
                _transactionUserInteraction.ShowMessage("What is the limit for this budget category?");
                string stringAmount = _transactionUserInteraction.GetInput();

                // TODO: try to set dictionary. Input might fail if tried twice
                double amount;
                if (double.TryParse(stringAmount, out amount))
                {
                    budgets[newCategory] = amount;
                    _transactionUserInteraction.ShowMessage($"{newCategory} set to ${amount.ToString("0.00")}");
                }

                

                /*
                    create a prompt user interaction function
                    get a dictionary of all the budget categories and their amounts

                    consider using the categories set by a profile, as categories in the output function
                */
            }
            else
            {
                isMoreCategories = false;
            }
        }

        BudgetProfile newBudgetProfile = new BudgetProfile(name, budgets, description);
        StoreProfile(name, newBudgetProfile);

        return newBudgetProfile;
    }
}
