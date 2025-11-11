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
            .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == categoryName.ToLower());

        if (existing != null)
        {
            return existing;
        }

        var newCategory = new Category
        {
            CategoryName = ToTitleCase(categoryName),
            CreatedDate = DateTime.UtcNow
        };

        await _context.Categories.AddAsync(newCategory);
        await _context.SaveChangesAsync();

        return newCategory;
    }

    public async Task SaveCategoryMappingAsync(string vendorName, string categoryName, TransactionType? suggestedType = null, bool overrideType = false, bool isTrackedOnly = false)
    {
        // Get or create the category
        var category = await GetOrCreateCategoryAsync(categoryName);

        // Update category's IsTrackedOnly flag
        if (category.IsTrackedOnly != isTrackedOnly)
        {
            category.IsTrackedOnly = isTrackedOnly;
            category.UpdatedDate = DateTime.UtcNow;
            _context.Categories.Update(category);
        }

        // Find the vendor mapping and update its category and transaction type
        var vendorMapping = await _context.VendorMappings
            .FirstOrDefaultAsync(v => v.VendorName.ToLower() == vendorName.ToLower());

        if (vendorMapping != null)
        {
            vendorMapping.CategoryId = category.Id;
            vendorMapping.SuggestedType = suggestedType;
            vendorMapping.OverrideType = overrideType;
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

    public async Task<Dictionary<string, int>> GetCategoryUsageCountsAsync()
    {
        // Get all categories with count of vendor mappings using each
        var categoryCounts = await _context.Categories
            .Include(c => c.Vendors)
            .Select(c => new
            {
                CategoryName = c.CategoryName,
                Count = c.Vendors.Count
            })
            .ToDictionaryAsync(x => x.CategoryName, x => x.Count);

        return categoryCounts;
    }

    public async Task MergeCategoriesAsync(List<string> oldCategoryNames, string canonicalName)
    {
        // Get or create the canonical category
        var canonicalCategory = await GetOrCreateCategoryAsync(canonicalName);

        // Find all categories to merge
        var categoriesToMerge = await _context.Categories
            .Where(c => oldCategoryNames.Contains(c.CategoryName))
            .ToListAsync();

        // Update all vendor mappings to use the canonical category
        foreach (var oldCategory in categoriesToMerge)
        {
            if (oldCategory.CategoryName == canonicalName)
            {
                continue; // Skip if it's already the canonical name
            }

            // Update all vendor mappings pointing to this category
            var vendorMappings = await _context.VendorMappings
                .Where(v => v.CategoryId == oldCategory.Id)
                .ToListAsync();

            foreach (var mapping in vendorMappings)
            {
                mapping.CategoryId = canonicalCategory.Id;
                mapping.UpdatedDate = DateTime.UtcNow;
            }

            // Remove the old category
            _context.Categories.Remove(oldCategory);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<string>> GetAllCategoryNamesAsync()
    {
        return await _context.Categories
            .Select(c => c.CategoryName)
            .ToListAsync();
    }

    public async Task<VendorMapping?> GetVendorMappingAsync(string vendorName)
    {
        return await _context.VendorMappings
            .Include(v => v.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.VendorName.ToLower() == vendorName.ToLower());
    }

    public async Task<Category?> GetCategoryByNameAsync(string categoryName)
    {
        return await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == categoryName.ToLower());
    }

    private string ToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
    }
}
