#### Personal Cashflow Categorizer

1. Update the program.cs file with a file path to a csv file with banking transactions
2. Compile and run the app
3. The app will ask you you to create a budget profile on the first use. Enter a profile name, description, and then add budget categories and the limits set for each month
4. Confirm the profile you've created is adiquate.
5. The application will convert each transaction into a transaction object with date, description, amounts for each expense.
6. The application will run a query on each description to determine what the vendor is. If a vendor can't be determined, the app will ask you to provide a substring matching the description and map it to a specific vendor. The key value pair will be saved for use for each future transaction.
7. Once all descriptions have been analyzed and a vendor has been associated with each, the app will associate a category with each of the vendors, and prompt the user for input when a new vendor is found.
8. Once all vendors have been categorized, a report is generated. The report shows all transactions for each date range, and individual reports for each category. The Expenses are compared to the budget set in the user's profile.
