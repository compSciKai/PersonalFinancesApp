using PersonalFinances.Repositories;
namespace PersonalFinances.App;

public class VendorsService : IVendorsService
{
    Dictionary<string, string>? VendorsMap {get; set;}
    IVendorsRepository _vendorsRepository;

    public VendorsService(IVendorsRepository vendorsRepository)
    {
        _vendorsRepository = vendorsRepository;
    }

    public void Init(string vendorsFilePath) 
    {
        VendorsMap = _vendorsRepository.LoadVendorsMap(vendorsFilePath);
    }

    public string GetVendor(string taransactionData)
    {
        foreach (var kvp in VendorsMap)
        {
            if (taransactionData.ToLower().Contains(kvp.Key.ToLower()))
            {
                return kvp.Value.ToLower();
            }
        }

        return "";
    }

    public void StoreNewVendor(string path, string key, string vendorName)
    {
        VendorsMap.Add(key, vendorName);
        _vendorsRepository.SaveVendorsMap(path, VendorsMap);
    }

    public void StoreNewVendors(Dictionary<string, string> newVendorsEntries)
    {
        foreach (var kvp in newVendorsEntries)
        {
            if (!newVendorsEntries.ContainsKey(kvp.Key))
            {
                VendorsMap[kvp.Key] = kvp.Value;
            }
        }
    }
}

