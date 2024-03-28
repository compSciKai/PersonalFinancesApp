using PersonalFinances.Repositories;

namespace PersonalFinances.App;

public class BudgetService : IBudgetService
{
    List<BudgetProfile> BudgetProfiles {get; set;}
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
        return BudgetProfiles.Find(profile => profile.Name == profileName);
    }

    public void StoreProfile(BudgetProfile profile)
    {
        BudgetProfiles.Add(profile);
        _budgetRepository.SaveBudgetProfiles(BudgetProfiles);
    }

    List<string> GetProfileNames()
    {
        List<string> profileNames = new List<string>();

        foreach (var profile in BudgetProfiles)
        {
            profileNames.Add(profile.Name);
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

        _transactionUserInteraction.ShowMessage("Set a description for this profile, or press enter.\n");
        string description = _transactionUserInteraction.GetInput();

        Dictionary<string, double> budgets = new Dictionary<string, double>();
        bool isMoreCategories = true;
        while (isMoreCategories)
        {
            _transactionUserInteraction.ShowMessage("Add a new budget category."
            + " press enter when finished.\n");
            string newCategory = _transactionUserInteraction.GetInput();

            if (newCategory != "")
            {
                _transactionUserInteraction.ShowMessage("What is the limit for this budget category?\n");
                string stringAmount = _transactionUserInteraction.GetInput();

                // TODO: try to set dictionary. Input might fail if tried twice
                double amount;
                if (double.TryParse(stringAmount, out amount))
                {
                    budgets[newCategory] = amount;
                    _transactionUserInteraction.ShowMessage($"\n{newCategory} set to ${amount.ToString("0.00")}");
                }
            }
            else
            {
                isMoreCategories = false;
            }
        }

        BudgetProfile newBudgetProfile = new BudgetProfile(name, budgets, description);
        StoreProfile(newBudgetProfile);

        return newBudgetProfile;
    }
}
