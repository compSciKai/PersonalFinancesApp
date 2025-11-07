using System.Data;
using DataTablePrettyPrinter;
using PersonalFinances.Models;

namespace PersonalFinances.App;

public class TransactionsConsoleUserInteraction : ITransactionsUserInteraction
{
    public TransactionsConsoleUserInteraction()
    {
    }

    public void ShowMessage(string message) 
    {
        Console.WriteLine(message);
    }

    public string GetInput() 
    {
        return Console.ReadLine();
    }

    public void Exit() 
    {
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
        System.Environment.Exit(1);
    }

    public string? PromptForProfileChoice(List<string> profileNames)
    {
        int nameCount = profileNames.Count;
        while (true)
        {
            ShowMessage("Which budget profile would you like to use?\n");
            for (int i = 0; i < nameCount; i++)
            {
                Console.WriteLine($"{i+1}. {profileNames[i]}");
            }

            ShowMessage($"{nameCount + 1}. Create New Profile");
            ShowMessage($"{nameCount + 2}. Exit");
            ShowMessage("");

            var input = GetInput();

            int choiceIndex;
            if(int.TryParse(input, out choiceIndex))
            {
                choiceIndex--;
                if (choiceIndex >= 0 && choiceIndex < nameCount)
                {
                    return profileNames[choiceIndex];
                }
                else if (choiceIndex == nameCount) // Create new profile
                {
                    return null;
                }
                else if (choiceIndex == nameCount + 1) // Exit App
                {
                    Exit();
                }
            }

            ShowMessage("Incorrect input. Please try again");
        }
    }

    public void OutputTransactions(List<Transaction> transactions, string tableName, BudgetProfile? profile)
    {
        // source: https://learn.microsoft.com/en-us/dotnet/api/system.data.datarow?view=net-8.0

        DataTable table = GenerateTransactionsTable(transactions, tableName);

        decimal totalExpenses = table.AsEnumerable().Sum(row => row.Field<decimal>("Amount"));

        DataRow subtotalsRow = table.NewRow();
        subtotalsRow["Description"] = "SUBTOTAL";
        subtotalsRow["Amount"] = totalExpenses;
        subtotalsRow["Vendor Name"] = "";
        subtotalsRow["Category"] = "";

        DataRow dividerRow = table.NewRow();
        dividerRow["Category"] = "";
        dividerRow["Vendor Name"] = "";

        table.Rows.Add(dividerRow);
        table.Rows.Add(subtotalsRow);

        // TODO: move to method -- calculate budget
        if (profile is not null && profile.BudgetCategories.ContainsKey(tableName))
        {
            decimal limit = (decimal)profile.BudgetCategories[tableName];
            decimal remaining = limit + totalExpenses;


            DataRow budgetLimitRow = table.NewRow();
            budgetLimitRow["Category"] = "";
            budgetLimitRow["Vendor Name"] = "";
            budgetLimitRow["Description"] = "BUDGET LIMIT";
            budgetLimitRow["Amount"] = limit.ToString("0.00");
            table.Rows.Add(budgetLimitRow);


            DataRow budgetRatioRow = table.NewRow();
            budgetRatioRow["Category"] = "";
            budgetRatioRow["Vendor Name"] = "";
            budgetRatioRow["Description"] = "BUDGET REMAINING";
            budgetRatioRow["Amount"] = remaining.ToString("0.00");
            table.Rows.Add(budgetRatioRow);
        }

        FormatTableWidths(table);
        table.SetTitleTextAlignment(TextAlignment.Left);

        Console.WriteLine(table.ToPrettyPrintedString());
    }

    private DataTable GenerateTransactionsTable(List<Transaction> transactions, string tableName)
    {
        DataTable table = MakeTransactionsTable(tableName.ToUpper());
        table.Columns[0].SetShowColumnName(false);

        foreach (var transaction in transactions)
        {
            string? vendor = transaction.Vendor?.ToUpper();
            string? category = transaction.Category?.ToUpper();

            DataRow row = table.NewRow();
            row["ID"] = transactions.IndexOf(transaction) + 1;
            row["Account Type"] = transaction.AccountType;
            row["Date"] = transaction.Date.ToShortDateString();
            row["Vendor Name"] = vendor;
            row["Category"] = category;
            row["Description"] = transaction.Description;

            if (!transaction.isNegativeAmounts)
            {
                row["Amount"] = (transaction.Amount * -1).ToString("0.00");
            }
            else
            {
                row["Amount"] = transaction.Amount.ToString("0.00");
            }

            table.Rows.Add(row);
        }

        return table;
    }

    private void FormatTableWidths(DataTable table)
    {
        table.Columns[0].SetWidth(4);
        table.Columns[1].SetWidth(12);
        table.Columns[2].SetWidth(15);
        table.Columns[3].SetWidth(25);
        table.Columns[4].SetWidth(18);
        table.Columns[5].SetWidth(42);
        table.Columns[6].SetWidth(11);
    }

    private DataTable MakeTransactionsTable(string tableName)
    {
        // source: https://learn.microsoft.com/en-us/dotnet/api/system.data.datarow?view=net-8.0

        // Create a new DataTable titled 'Transactions.'
        DataTable transactionsTable = new DataTable(tableName);

        // Add column objects to the table.
        DataColumn idColumn = new  DataColumn();
        idColumn.DataType = System.Type.GetType("System.Int32");
        idColumn.ColumnName = "ID";
        idColumn.AutoIncrement = true;
        transactionsTable.Columns.Add(idColumn);

        DataColumn accountTypeColumn = new  DataColumn();
        accountTypeColumn.DataType = System.Type.GetType("System.String");
        accountTypeColumn.ColumnName = "Account Type";
        transactionsTable.Columns.Add(accountTypeColumn);

        DataColumn dateColumn = new  DataColumn();
        dateColumn.DataType = System.Type.GetType("System.DateTime");
        dateColumn.ColumnName = "Date";
        transactionsTable.Columns.Add(dateColumn);

        DataColumn vNameColumn = new DataColumn();
        vNameColumn.DataType = System.Type.GetType("System.String");
        vNameColumn.ColumnName = "Vendor Name";
        vNameColumn.DefaultValue = "Vendor Name";
        transactionsTable.Columns.Add(vNameColumn);

        DataColumn categoryColumn = new DataColumn();
        categoryColumn.DataType = System.Type.GetType("System.String");
        categoryColumn.ColumnName = "Category";
        categoryColumn.DefaultValue = "Category";
        transactionsTable.Columns.Add(categoryColumn);

        DataColumn descriptionColumn = new DataColumn();
        descriptionColumn.DataType = System.Type.GetType("System.String");
        descriptionColumn.ColumnName = "Description";
        transactionsTable.Columns.Add(descriptionColumn);

        DataColumn amountColumn = new DataColumn();
        amountColumn.DataType = System.Type.GetType("System.Decimal");
        amountColumn.ColumnName = "Amount";
        amountColumn.SetDataAlignment(TextAlignment.Right);
        transactionsTable.Columns.Add(amountColumn);

        // Create an array for DataColumn objects.
        DataColumn [] keys = new DataColumn [1];
        keys[0] = idColumn;
        transactionsTable.PrimaryKey = keys;

        // Return the new DataTable.
        return transactionsTable;
    }

    public (KeyValuePair<string, string>? kvp, bool skipAll) PromptForVendorKVP(string description)
    {
        bool invalidVendorInput = true;
        string vendorKey = "";
        string vendorValue = "";

        ShowMessage(
        $"Vendor could not be found for this transaction with description: '{description}'.");

        while (invalidVendorInput)
        {
            ShowMessage($"Enter a string from the description that will identify the vendor for this transaction,");
            ShowMessage($"or press Enter to save as '{description}', or type 's' to skip, or 'sa' to skip all");
            vendorKey = GetInput();

            if (vendorKey == "s")
            {
                return (null, false);
            }

            if (vendorKey == "sa")
            {
                return (null, true);
            }

            // If user presses Enter, use the full description
            if (string.IsNullOrEmpty(vendorKey))
            {
                return (new KeyValuePair<string, string>(description.ToLower(), description.ToLower()), false);
            }

            if (description.ToLower().Contains(vendorKey.ToLower()))
            {
                ShowMessage($"What is the vendor's name for this transaction? Press enter to save as '{vendorKey}'");
                vendorValue = GetInput();

                if (vendorValue is "")
                {
                    vendorValue = vendorKey;
                }

                invalidVendorInput = false;
            }
            else
            {
                ShowMessage("That vendor is not present in the description. Try again.");
            }
        }

        return (new KeyValuePair<string, string>(vendorKey.ToLower(), vendorValue.ToLower()), false);
    }

    public (KeyValuePair<string, string>? kvp, bool skipAll, bool addToBudget) PromptForCategoryKVP(string vendor, BudgetProfile? profile)
    {
        bool invalidCategoryInput = true;
        string categoryKey = "";
        string categoryValue = "";
        bool addToBudget = false;

        ShowMessage($"\nCategory could not be found for this vendor: '{vendor}'.");

        // Show autocomplete suggestions (Phase 3)
        if (profile != null && profile.BudgetCategories.Any())
        {
            ShowMessage("\nYour budget categories:");
            int index = 1;
            foreach (var category in profile.BudgetCategories.Keys.OrderBy(c => c))
            {
                ShowMessage($"  {index}. {category}");
                index++;
            }
            ShowMessage("");
        }

        while (invalidCategoryInput)
        {
            ShowMessage($"Enter a string from the vendor that will identify the category for this vendor,");
            ShowMessage($"or press Enter to use '{vendor}', or type 's' to skip, or 'sa' to skip all");
            categoryKey = GetInput();

            if (categoryKey == "s")
            {
                return (null, false, false);
            }

            if (categoryKey == "sa")
            {
                return (null, true, false);
            }

            // If user presses Enter, use the full vendor name as the pattern
            if (string.IsNullOrEmpty(categoryKey))
            {
                categoryKey = vendor;
            }

            if (vendor.ToLower().Contains(categoryKey.ToLower()))
            {
                ShowMessage($"What is the category for this vendor? Press enter to save as '{categoryKey}'");
                categoryValue = GetInput();

                if (categoryValue is "")
                {
                    categoryValue = categoryKey;
                }

                // Phase 2: Validate against budget profile
                if (profile != null)
                {
                    // Check for case-insensitive match
                    var matchingCategory = profile.BudgetCategories.Keys
                        .FirstOrDefault(c => c.Equals(categoryValue, StringComparison.OrdinalIgnoreCase));

                    if (matchingCategory != null && !matchingCategory.Equals(categoryValue))
                    {
                        // Found a case-insensitive match - suggest it
                        ShowMessage($"\nDid you mean '{matchingCategory}'? (y/n)");
                        string response = GetInput().Trim().ToLower();

                        if (response == "y" || response == "yes" || string.IsNullOrEmpty(response))
                        {
                            categoryValue = matchingCategory;
                        }
                    }
                    else if (matchingCategory == null)
                    {
                        // Category not found in budget - show all categories and prompt to add
                        ShowMessage($"\nCategory '{categoryValue}' not found in budget.");
                        ShowMessage("Your current budget categories are:");
                        foreach (var cat in profile.BudgetCategories.Keys.OrderBy(c => c))
                        {
                            ShowMessage($"  - {cat}");
                        }
                        ShowMessage($"\nWould you like to add '{categoryValue}' to your budget? (y/n/s/sa)");
                        string addResponse = GetInput().Trim().ToLower();

                        if (addResponse == "y" || addResponse == "yes")
                        {
                            addToBudget = true;
                        }
                        else if (addResponse == "s")
                        {
                            return (null, false, false);
                        }
                        else if (addResponse == "sa")
                        {
                            return (null, true, false);
                        }
                    }
                }

                invalidCategoryInput = false;
            }
            else
            {
                ShowMessage("That value is not present in the vendor name. Try again.");
            }
        }

        return (new KeyValuePair<string, string>(categoryKey.ToLower(), categoryValue.ToLower()), false, addToBudget);
    }
    public void OutputBudgetVsActual(List<Transaction> transactions, BudgetProfile? profile)
    {
        DataTable table = GenerateTransactionsTable(transactions, "Budget vs Actual");

        if (profile == null)
        {
            ShowMessage("No budget profile provided. Cannot calculate budget vs actual.");
            return;
        }

        // Create the final table with required columns
        DataTable finalTable = new DataTable("Budget vs Actual");
        finalTable.Columns.Add("Category", typeof(string));
        finalTable.Columns.Add("Budgeted", typeof(decimal));
        finalTable.Columns.Add("Actual", typeof(decimal));
        finalTable.Columns.Add("Difference", typeof(decimal));

        // Calculate actual amounts from the input table
        var actualAmounts = table.AsEnumerable()
            .Where(row => row["Category"] is not DBNull && row["Amount"] is not DBNull)
            .GroupBy(row => row["Category"].ToString().ToLower())
            .ToDictionary(
                group => group.Key,
                group => group.Sum(row => Convert.ToDecimal(row["Amount"]))
            );

        // Populate the final table
        foreach (var budgetCategory in profile.BudgetCategories)
        {
            string category = budgetCategory.Key;
            decimal budgeted = (decimal)budgetCategory.Value;
            decimal actual = actualAmounts.ContainsKey(category) ? actualAmounts[category] : 0;
            decimal difference = budgeted + actual; // actual in negative figure

            DataRow row = finalTable.NewRow();
            row["Category"] = category;
            row["Budgeted"] = budgeted;
            row["Actual"] = actual;
            row["Difference"] = difference;
            finalTable.Rows.Add(row);
        }

        // Format and print the final table
        finalTable.SetTitleTextAlignment(TextAlignment.Left);
        finalTable.Columns["Actual"].SetDataAlignment(TextAlignment.Right);
        finalTable.Columns["Difference"].SetDataAlignment(TextAlignment.Right);

        Console.WriteLine(finalTable.ToPrettyPrintedString());
    }

    public double PromptForBudgetAmount(string categoryName, double remainingBudget)
    {
        ShowMessage($"\nRemaining budget available: ${remainingBudget:0.00}");

        while (true)
        {
            ShowMessage($"Enter budget amount for '{categoryName}': ");
            string input = GetInput();

            if (double.TryParse(input, out double amount))
            {
                if (amount <= 0)
                {
                    ShowMessage("Budget amount must be positive. Try again.");
                    continue;
                }

                if (amount > remainingBudget)
                {
                    ShowMessage($"Amount exceeds remaining budget (${remainingBudget:0.00}). Try again.");
                    continue;
                }

                return amount;
            }
            else
            {
                ShowMessage("Invalid input. Please enter a valid number.");
            }
        }
    }
}
