using PersonalFinances.Data;
using PersonalFinances.Models;
using Microsoft.EntityFrameworkCore;

namespace PersonalFinances.Utilities;

/// <summary>
/// Utility to detect and resolve duplicate category names in the database.
/// Duplicates can occur due to case variations (e.g., "Groceries" vs "groceries")
/// or whitespace differences.
/// </summary>
public class CategoryDuplicateDetector
{
    private readonly TransactionContext _context;

    public CategoryDuplicateDetector(TransactionContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Detects duplicate category names (case-insensitive comparison).
    /// </summary>
    public async Task<List<DuplicateGroup>> DetectDuplicatesAsync()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        var duplicateGroups = categories
            .GroupBy(c => c.CategoryName.Trim().ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateGroup
            {
                NormalizedName = g.Key,
                Categories = g.ToList()
            })
            .ToList();

        return duplicateGroups;
    }

    /// <summary>
    /// Displays duplicate categories and provides interactive resolution.
    /// </summary>
    /// <param name="autoResolve">If true, automatically resolves duplicates without prompting. If false/null, prompts user.</param>
    public async Task<bool> DetectAndResolveInteractiveAsync(bool? autoResolve = null)
    {
        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          Category Duplicate Detection & Resolution           ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

        var duplicates = await DetectDuplicatesAsync();

        if (duplicates.Count == 0)
        {
            Console.WriteLine("✓ No duplicate categories found! Your database is clean.\n");
            return true;
        }

        Console.WriteLine($"⚠ Found {duplicates.Count} duplicate category group(s):\n");

        // Display all duplicates
        int groupNumber = 1;
        foreach (var group in duplicates)
        {
            Console.WriteLine($"Group {groupNumber}: '{group.NormalizedName}' has {group.Categories.Count} variations:");
            foreach (var category in group.Categories)
            {
                var vendorCount = await _context.VendorMappings
                    .Where(v => v.CategoryId == category.Id)
                    .CountAsync();

                Console.WriteLine($"  [{category.Id}] \"{category.CategoryName}\" (IsTrackedOnly: {category.IsTrackedOnly}, Vendors: {vendorCount})");
            }
            Console.WriteLine();
            groupNumber++;
        }

        // If autoResolve is specified, use it
        if (autoResolve.HasValue)
        {
            if (autoResolve.Value)
            {
                Console.WriteLine("Auto-resolving duplicates...\n");
                return await AutoResolveAsync(duplicates);
            }
            else
            {
                Console.WriteLine("Exiting without changes (auto-resolve = false).");
                return false;
            }
        }

        // Ask user if they want to auto-resolve
        Console.WriteLine("Options:");
        Console.WriteLine("  1. Auto-resolve (keep first, merge vendors, delete others)");
        Console.WriteLine("  2. Manual resolution (choose which to keep for each group)");
        Console.WriteLine("  3. Exit without changes");
        Console.Write("\nSelect option (1-3): ");

        var choice = Console.ReadLine()?.Trim();

        switch (choice)
        {
            case "1":
                return await AutoResolveAsync(duplicates);
            case "2":
                return await ManualResolveAsync(duplicates);
            case "3":
                Console.WriteLine("Exiting without changes.");
                return false;
            default:
                Console.WriteLine("Invalid option. Exiting without changes.");
                return false;
        }
    }

    /// <summary>
    /// Automatically resolves duplicates by keeping the first entry and merging vendors.
    /// </summary>
    private async Task<bool> AutoResolveAsync(List<DuplicateGroup> duplicates)
    {
        Console.WriteLine("\n--- Auto-Resolution ---");
        Console.WriteLine("Strategy: Keep first entry, reassign vendors, delete duplicates\n");

        int totalResolved = 0;

        foreach (var group in duplicates)
        {
            var categoryToKeep = group.Categories.First();
            var categoriesToRemove = group.Categories.Skip(1).ToList();

            Console.WriteLine($"Processing '{categoryToKeep.CategoryName}' (ID: {categoryToKeep.Id})...");

            // Reassign vendors from duplicates to the category we're keeping
            foreach (var duplicate in categoriesToRemove)
            {
                var vendors = await _context.VendorMappings
                    .Where(v => v.CategoryId == duplicate.Id)
                    .ToListAsync();

                foreach (var vendor in vendors)
                {
                    vendor.CategoryId = categoryToKeep.Id;
                }

                Console.WriteLine($"  - Reassigned {vendors.Count} vendor(s) from '{duplicate.CategoryName}' (ID: {duplicate.Id})");

                // Remove the duplicate category
                _context.Categories.Remove(duplicate);
            }

            totalResolved++;
        }

        await _context.SaveChangesAsync();

        Console.WriteLine($"\n✓ Successfully resolved {totalResolved} duplicate group(s)!");
        return true;
    }

    /// <summary>
    /// Allows user to manually choose which category to keep for each duplicate group.
    /// </summary>
    private async Task<bool> ManualResolveAsync(List<DuplicateGroup> duplicates)
    {
        Console.WriteLine("\n--- Manual Resolution ---\n");

        int totalResolved = 0;

        foreach (var group in duplicates)
        {
            Console.WriteLine($"Group: '{group.NormalizedName}' ({group.Categories.Count} variations)");

            for (int i = 0; i < group.Categories.Count; i++)
            {
                var category = group.Categories[i];
                var vendorCount = await _context.VendorMappings
                    .Where(v => v.CategoryId == category.Id)
                    .CountAsync();

                Console.WriteLine($"  {i + 1}. [{category.Id}] \"{category.CategoryName}\" (IsTrackedOnly: {category.IsTrackedOnly}, Vendors: {vendorCount})");
            }

            Console.Write($"\nWhich one to keep? (1-{group.Categories.Count}, or 's' to skip): ");
            var input = Console.ReadLine()?.Trim().ToLower();

            if (input == "s")
            {
                Console.WriteLine("Skipped.\n");
                continue;
            }

            if (int.TryParse(input, out int selectedIndex) && selectedIndex >= 1 && selectedIndex <= group.Categories.Count)
            {
                var categoryToKeep = group.Categories[selectedIndex - 1];
                var categoriesToRemove = group.Categories.Where((c, idx) => idx != selectedIndex - 1).ToList();

                Console.WriteLine($"Keeping '{categoryToKeep.CategoryName}' (ID: {categoryToKeep.Id})");

                // Reassign vendors
                foreach (var duplicate in categoriesToRemove)
                {
                    var vendors = await _context.VendorMappings
                        .Where(v => v.CategoryId == duplicate.Id)
                        .ToListAsync();

                    foreach (var vendor in vendors)
                    {
                        vendor.CategoryId = categoryToKeep.Id;
                    }

                    Console.WriteLine($"  - Reassigned {vendors.Count} vendor(s) from '{duplicate.CategoryName}' (ID: {duplicate.Id})");

                    _context.Categories.Remove(duplicate);
                }

                totalResolved++;
                Console.WriteLine("Resolved!\n");
            }
            else
            {
                Console.WriteLine("Invalid selection. Skipping this group.\n");
            }
        }

        if (totalResolved > 0)
        {
            await _context.SaveChangesAsync();
            Console.WriteLine($"\n✓ Successfully resolved {totalResolved} duplicate group(s)!");
            return true;
        }
        else
        {
            Console.WriteLine("\nNo changes made.");
            return false;
        }
    }

    /// <summary>
    /// Represents a group of duplicate categories.
    /// </summary>
    public class DuplicateGroup
    {
        public string NormalizedName { get; set; } = string.Empty;
        public List<Category> Categories { get; set; } = new();
    }
}
