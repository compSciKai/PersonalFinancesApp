using PersonalFinances.Models;
using PersonalFinances.Repositories;
namespace PersonalFinances.App;

public class VendorsService : IVendorsService
{
    Dictionary<string, string> VendorsMap {get; set;}
    IVendorsRepository _vendorsRepository;
    ITransactionsUserInteraction _transactionUserInteraction;

    public VendorsService(
        IVendorsRepository vendorsRepository,
        ITransactionsUserInteraction transactionUserInteraction)
    {
        _vendorsRepository = vendorsRepository;
        _transactionUserInteraction = transactionUserInteraction;
        VendorsMap = vendorsRepository.LoadVendorsMap();
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

    public void StoreNewVendor(string key, string vendorName)
    {
        VendorsMap.Add(key, vendorName);
        _vendorsRepository.SaveVendorsMap(VendorsMap);
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

    public List<Transaction> AddVendorsToTransactions(List<Transaction> transactions)
    {
        foreach (var transaction in transactions)
        {
            if (transaction.Vendor is null)
            {
                string? vendorName = GetVendor(transaction.Description);

                if (vendorName == "")
                {
                    KeyValuePair<string, string>? vendorKVP = _transactionUserInteraction.PromptForVendorKVP(transaction.Description);
                    if (vendorKVP is null)
                    {
                        continue;
                    }

                    StoreNewVendor(vendorKVP?.Key, vendorKVP?.Value);
                    vendorName = vendorKVP?.Value;
                }

                transaction.Vendor = vendorName;
            }
        }

        return transactions;
    }
}

