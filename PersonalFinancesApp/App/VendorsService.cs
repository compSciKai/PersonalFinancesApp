using PersonalFinances.Repositories;
namespace PersonalFinances.App;

public class VendorsService : IVendorsService
{
    Dictionary<string, string> _vendorsMap;
    IVendorsRepository _vendorsRepository;

    VendorsService(IVendorsRepository vendorsRepository)
    {
        _vendorsRepository = vendorsRepository;

        // load vendors repo and initialize map

        // IEnumerable<string> vendorStrings = _vendorsRepository.Read();
        // foreach (var vendorString in vendorStrings)
        // {
        //     // Split the string and add key value pairs
        // }
    }

    void SaveNewVendors(Dictionary<string, string> newVendorMap, string path);
    string FindVendorFromDescription(string transactionDescription);
    string CreateVendor(string key, string vendorName);
}

