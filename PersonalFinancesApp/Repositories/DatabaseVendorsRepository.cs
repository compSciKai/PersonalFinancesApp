using Microsoft.EntityFrameworkCore;
using PersonalFinances.Data;
using PersonalFinances.Models;

namespace PersonalFinances.Repositories;

public class DatabaseVendorsRepository : IVendorsRepository
{
    private readonly TransactionContext _context;

    public DatabaseVendorsRepository(TransactionContext context)
    {
        _context = context;
    }

    public Dictionary<string, string> LoadVendorsMap()
    {
        var vendors = _context.VendorMappings.AsNoTracking().ToList();
        return vendors.ToDictionary(v => v.Pattern, v => v.VendorName);
    }

    public void SaveVendorsMap(Dictionary<string, string> map)
    {
        throw new NotImplementedException(
            "SaveVendorsMap is not supported for database repository. " +
            "Use SaveVendorMappingAsync for individual vendors.");
    }

    public async Task SaveVendorMappingAsync(VendorMapping vendorMapping)
    {
        var existing = await _context.VendorMappings
            .FirstOrDefaultAsync(v => v.Pattern.ToLower() == vendorMapping.Pattern.ToLower());

        if (existing != null)
        {
            existing.VendorName = ToTitleCase(vendorMapping.VendorName);
            existing.CategoryId = vendorMapping.CategoryId;
            existing.UpdatedDate = DateTime.UtcNow;
            _context.VendorMappings.Update(existing);
        }
        else
        {
            vendorMapping.VendorName = ToTitleCase(vendorMapping.VendorName);
            vendorMapping.CreatedDate = DateTime.UtcNow;
            await _context.VendorMappings.AddAsync(vendorMapping);
        }

        await _context.SaveChangesAsync();
    }

    public async Task SaveVendorMappingsAsync(List<VendorMapping> vendorMappings)
    {
        foreach (var vendorMapping in vendorMappings)
        {
            vendorMapping.VendorName = ToTitleCase(vendorMapping.VendorName);
            vendorMapping.CreatedDate = DateTime.UtcNow;
        }

        await _context.VendorMappings.AddRangeAsync(vendorMappings);
        await _context.SaveChangesAsync();
    }

    private string ToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
    }
}
