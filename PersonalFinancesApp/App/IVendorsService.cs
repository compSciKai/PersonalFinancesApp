using PersonalFinances.Models;

namespace PersonalFinances.App;

public interface IVendorsService
{
    string GetVendor(string transactionData);
    void StoreNewVendor(string key, string vendorName);
    void StoreNewVendors(Dictionary<string, string> vendorsDictionary);
    List<Transaction> AddVendorsToTransactions(List<Transaction> rawTransactions);
}
