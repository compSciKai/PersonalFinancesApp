using PersonalFinances.Models;
using PersonalFinances.Repositories;
namespace PersonalFinances.App;

public class VendorsService : IVendorsService
{
    Dictionary<string, string> VendorsMap {get; set;}
    IVendorsRepository _vendorsRepository;
    IVendorsRepository? _jsonRepository;
    ITransactionsUserInteraction _transactionUserInteraction;

    public VendorsService(
        IVendorsRepository vendorsRepository,
        IVendorsRepository? jsonRepository,
        ITransactionsUserInteraction transactionUserInteraction)
    {
        _vendorsRepository = vendorsRepository;
        _jsonRepository = jsonRepository;
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

    public async Task MigrateVendorsFromJsonAsync()
    {
        if (_jsonRepository == null)
        {
            _transactionUserInteraction.ShowMessage("Error: JSON repository not available for migration.");
            return;
        }

        _transactionUserInteraction.ShowMessage("\n=== Migrating Vendors from JSON to Database ===");

        try
        {
            // Load vendors from JSON
            var jsonVendorsMap = _jsonRepository.LoadVendorsMap();
            _transactionUserInteraction.ShowMessage($"Loaded {jsonVendorsMap.Count} vendors from JSON file.");

            // Convert to VendorMapping entities
            var vendorMappings = jsonVendorsMap.Select(kvp => new VendorMapping
            {
                Pattern = kvp.Key,
                VendorName = kvp.Value,
                CategoryId = null  // Will be set during category migration
            }).ToList();

            // Save to database
            var dbRepo = _vendorsRepository as DatabaseVendorsRepository;
            if (dbRepo != null)
            {
                await dbRepo.SaveVendorMappingsAsync(vendorMappings);
                _transactionUserInteraction.ShowMessage($"Successfully migrated {vendorMappings.Count} vendors to database.");
            }
            else
            {
                _transactionUserInteraction.ShowMessage("Error: Database repository not available.");
            }
        }
        catch (Exception ex)
        {
            _transactionUserInteraction.ShowMessage($"Error during vendor migration: {ex.Message}");
            throw;
        }
    }
}

