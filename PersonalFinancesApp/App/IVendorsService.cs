namespace PersonalFinances.App;

public interface IVendorsService
{
    void LoadVendors(string path);
    void SaveVendors(string path);
    string FindVendor(string transactionDescription);
    string CreateVendor(string key, string vendorName);
}
