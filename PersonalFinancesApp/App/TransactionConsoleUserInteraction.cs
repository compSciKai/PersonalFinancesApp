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

        DataTable table;
        table = MakeTransactionsTable(tableName.ToUpper());
        table.Columns[0].SetShowColumnName(false);

        for (int i = 0; i < transactions.Count(); i++)
        {
            string vendor = transactions[i].Vendor.ToUpper();
            string category = transactions[i].Category.ToUpper();


            DataRow row = table.NewRow();
            row["ID"] = i+1;
            row["Date"] = transactions[i].Date.ToShortDateString();
            row["Vendor Name"] = vendor;
            row["Category"] = category;
            row["Description"] = transactions[i].Description;
            row["Amount"] = transactions[i].Amount.ToString("0.00");

            table.Rows.Add(row);
        }


        decimal totalExpenses = table.AsEnumerable().Where(row => row.Field<string>("Vendor Name") != "VISA PAYMENT").Sum(row => row.Field<decimal>("Amount"));

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

    private void FormatTableWidths(DataTable table)
    {
        table.Columns[0].SetWidth(4);
        table.Columns[1].SetWidth(15);
        table.Columns[2].SetWidth(25);
        table.Columns[3].SetWidth(18);
        table.Columns[4].SetWidth(42);
        table.Columns[5].SetWidth(11);




    }

    private DataTable MakeSubtotalsTable()
    {
        // source: https://learn.microsoft.com/en-us/dotnet/api/system.data.datarow?view=net-8.0

        // Create a new DataTable titled 'Transactions.'
        DataTable subtotalsTable = new DataTable("Subtotals");

        // Add column objects to the table.
        DataColumn idColumn = new  DataColumn();
        idColumn.DataType = System.Type.GetType("System.Int32");
        idColumn.ColumnName = "ID";
        idColumn.AutoIncrement = true;
        subtotalsTable.Columns.Add(idColumn);

        DataColumn typeColumn = new  DataColumn();
        typeColumn.DataType = System.Type.GetType("System.String");
        typeColumn.ColumnName = "Type";
        subtotalsTable.Columns.Add(typeColumn);

        DataColumn amountColumn = new DataColumn();
        amountColumn.DataType = System.Type.GetType("System.Decimal");
        amountColumn.ColumnName = "Amount";
        amountColumn.SetDataAlignment(TextAlignment.Right);
        subtotalsTable.Columns.Add(amountColumn);

        // Create an array for DataColumn objects.
        DataColumn [] keys = new DataColumn [1];
        keys[0] = idColumn;
        subtotalsTable.PrimaryKey = keys;

        // Return the new DataTable.
        return subtotalsTable;
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

    public KeyValuePair<string, string>? PromptForVendorKVP(string description)
    {
        bool invalidVendorInput = true;
        string vendorKey = ""; 
        string vendorValue = "";

        ShowMessage(
        $"Vendor could not be found for this transaction with description: '{description}'.");

        while (invalidVendorInput)
        {
            ShowMessage("Enter a string from the description that will identify the vendor for this transaction, or type 's' to skip");
            vendorKey = GetInput();
            
            if (vendorKey == "s")
            {
                return null;
            }

            if (vendorKey is not "" && description.ToLower().Contains(vendorKey.ToLower()))
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

        return new KeyValuePair<string, string>(vendorKey.ToLower(), vendorValue.ToLower());
    }

    public KeyValuePair<string, string>? PromptForCategoryKVP(string vendor)
    {
        bool invalidCategoryInput = true;
        string categoryKey = ""; 
        string categoryValue = "";

        ShowMessage(
        $"Category could not be found for this vendor: '{vendor}'.");

        while (invalidCategoryInput)
        {
            ShowMessage("Enter a string from the vendor that will identify the category for this vendor, or type 's' to skip");
            categoryKey = GetInput();
            
            if (categoryKey == "s")
            {
                return null;
            }

            if (categoryKey is not "" && vendor.ToLower().Contains(categoryKey.ToLower()))
            {
                ShowMessage($"What is the category for this vendor? Press enter to save as '{categoryKey}'");
                categoryValue = GetInput();

                if (categoryValue is "")
                {
                    categoryValue = categoryKey;
                }

                invalidCategoryInput = false; 
            }
            else 
            {
                ShowMessage("That value is not present in the vendor name. Try again.");
            }
        }

        return new KeyValuePair<string, string>(categoryKey.ToLower(), categoryValue.ToLower());
    }
}
