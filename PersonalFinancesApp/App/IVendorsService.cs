using PersonalFinances.Repositories;

namespace PersonalFinances.App;

public interface IVendorsService
{
    void Init(string VendorsFilePath);
    string GetVendor(string transactionData);
    void StoreNewVendor(string path, string key, string vendorName);
    void StoreNewVendors(Dictionary<string, string> vendorsDictionary);
}
