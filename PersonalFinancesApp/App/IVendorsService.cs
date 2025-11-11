using PersonalFinances.Models;

namespace PersonalFinances.App;

public interface IVendorsService
{
    string GetVendor(string transactionData);
    Task StoreNewVendorAsync(string key, string vendorName, TransactionType? suggestedType = null, bool overrideType = false);
    void StoreNewVendors(Dictionary<string, string> vendorsDictionary);
    Task<List<Transaction>> AddVendorsToTransactionsAsync(List<Transaction> rawTransactions);
    Task MigrateVendorsFromJsonAsync();
}
