using Microsoft.EntityFrameworkCore;
using PersonalFinances.Data;
using PersonalFinances.Models;

namespace PersonalFinances.Repositories;

public class DatabaseCategoriesRepository : ICategoriesRepository
{
    private readonly TransactionContext _context;

    public DatabaseCategoriesRepository(TransactionContext context)
    {
        _context = context;
    }

    public Dictionary<string, string> LoadCategoriesMap()
    {
        // Load vendor mappings with their categories
        var vendorsWithCategories = _context.VendorMappings
            .Include(v => v.Category)
            .AsNoTracking()
            .Where(v => v.Category != null)
            .ToList();

        // Build dictionary mapping vendorName -> categoryName
        return vendorsWithCategories.ToDictionary(
            v => v.VendorName,
            v => v.Category!.CategoryName
        );
    }

    public void SaveCategoriesMap(Dictionary<string, string> map)
    {
        throw new NotImplementedException(
            "SaveCategoriesMap is not supported for database repository. " +
            "Use SaveCategoryMappingAsync for individual categories.");
    }

    public async Task<Category> GetOrCreateCategoryAsync(string categoryName)
    {
        var existing = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryName == categoryName);

        if (existing != null)
        {
            return existing;
        }

        var newCategory = new Category
        {
            CategoryName = categoryName,
            CreatedDate = DateTime.UtcNow
        };

        await _context.Categories.AddAsync(newCategory);
        await _context.SaveChangesAsync();

        return newCategory;
    }

    public async Task SaveCategoryMappingAsync(string vendorName, string categoryName)
    {
        // Get or create the category
        var category = await GetOrCreateCategoryAsync(categoryName);

        // Find the vendor mapping and update its category
        var vendorMapping = await _context.VendorMappings
            .FirstOrDefaultAsync(v => v.VendorName == vendorName);

        if (vendorMapping != null)
        {
            vendorMapping.CategoryId = category.Id;
            vendorMapping.UpdatedDate = DateTime.UtcNow;
            _context.VendorMappings.Update(vendorMapping);
            await _context.SaveChangesAsync();
        }
    }

    public async Task SaveCategoryMappingsAsync(Dictionary<string, string> mappings)
    {
        foreach (var kvp in mappings)
        {
            await SaveCategoryMappingAsync(kvp.Key, kvp.Value);
        }
    }
}
