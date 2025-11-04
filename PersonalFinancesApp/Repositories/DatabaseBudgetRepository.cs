using Microsoft.EntityFrameworkCore;
using PersonalFinances.Data;
using PersonalFinances.Models;

namespace PersonalFinances.Repositories;

public class DatabaseBudgetRepository : IBudgetRepository
{
    private readonly TransactionContext _context;

    public DatabaseBudgetRepository(TransactionContext context)
    {
        _context = context;
    }

    public async Task<List<BudgetProfile>> LoadBudgetProfilesAsync()
    {
        return await _context.BudgetProfiles
            .Include(p => p.Categories)
            .ToListAsync();
    }

    public async Task SaveBudgetProfilesAsync(List<BudgetProfile> profiles)
    {
        foreach (var profile in profiles)
        {
            await SaveBudgetProfileAsync(profile);
        }
    }

    public async Task<BudgetProfile?> GetProfileByIdAsync(int id)
    {
        return await _context.BudgetProfiles
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<BudgetProfile?> GetProfileByNameAsync(string name)
    {
        return await _context.BudgetProfiles
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task SaveBudgetProfileAsync(BudgetProfile profile)
    {
        var existing = await _context.BudgetProfiles
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == profile.Id);

        if (existing != null)
        {
            // Update existing profile
            existing.Name = profile.Name;
            existing.UserName = profile.UserName;
            existing.Description = profile.Description;
            existing.Income = profile.Income;
            existing.UpdatedDate = DateTime.UtcNow;

            // Remove old categories
            _context.BudgetCategories.RemoveRange(existing.Categories);

            // Add new categories
            foreach (var category in profile.Categories)
            {
                category.BudgetProfileId = existing.Id;
                existing.Categories.Add(category);
            }

            _context.BudgetProfiles.Update(existing);
        }
        else
        {
            // Add new profile
            profile.CreatedDate = DateTime.UtcNow;
            foreach (var category in profile.Categories)
            {
                category.CreatedDate = DateTime.UtcNow;
            }
            await _context.BudgetProfiles.AddAsync(profile);
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_BudgetCategory_ProfileId_CategoryName") == true)
        {
            // Extract duplicate category names from the exception if possible
            var duplicates = profile.Categories
                .GroupBy(c => c.CategoryName, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            var duplicateList = duplicates.Any() ? string.Join(", ", duplicates) : "unknown categories";

            throw new InvalidOperationException(
                $"Cannot save budget profile '{profile.Name}': Duplicate category names detected ({duplicateList}). " +
                $"Each category must have a unique name within the profile.",
                ex);
        }
    }

    public async Task DeleteBudgetProfileAsync(int id)
    {
        var profile = await _context.BudgetProfiles.FindAsync(id);
        if (profile != null)
        {
            _context.BudgetProfiles.Remove(profile);
            await _context.SaveChangesAsync();
        }
    }
}
