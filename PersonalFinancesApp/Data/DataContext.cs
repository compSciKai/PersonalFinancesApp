using Microsoft.EntityFrameworkCore;
using PersonalFinances.Models;
using Npgsql;

namespace PersonalFinances.Data;

public class  TransactionContext : DbContext
{
    public DbSet<RBCTransaction> RBCTransactions { get; set; }
    public DbSet<AmexTransaction> AmexTransactions { get; set; }
    public DbSet<PCFinancialTransaction> PCTransactions { get; set; }
    public DbSet<BudgetProfile> BudgetProfiles { get; set; }
    public DbSet<BudgetCategory> BudgetCategories { get; set; }
    public DbSet<VendorMapping> VendorMappings { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            // Enable legacy timestamp behavior to allow DateTime with Kind=Unspecified
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            options.UseNpgsql(
                @"Host=aws-1-ca-central-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.vfzsxkzjubfrgcnbifpt;Password=uRI6FH23O5c8JYEt;SslMode=Require;",
                o => o.EnableRetryOnFailure());
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RBCTransaction>(entity =>
        {
            entity.ToTable("RBCTransactions");
            entity.Property(t => t.Description).HasMaxLength(200).IsRequired();
            entity.Property(t => t.TransactionHash).HasMaxLength(100);
            entity.HasIndex(t => t.TransactionHash).IsUnique();
        });

        modelBuilder.Entity<AmexTransaction>(entity =>
        {
            entity.ToTable("AmexTransactions");
            entity.Property(t => t.Description).HasMaxLength(200).IsRequired();
            entity.Property(t => t.TransactionHash).HasMaxLength(100);
            entity.HasIndex(t => t.TransactionHash).IsUnique();
        });

        modelBuilder.Entity<PCFinancialTransaction>(entity =>
        {
            entity.ToTable("PCFinancialTransactions");
            entity.Property(t => t.Description).HasMaxLength(200).IsRequired();
            entity.Property(t => t.TransactionHash).HasMaxLength(100);
            entity.HasIndex(t => t.TransactionHash).IsUnique();
        });

        modelBuilder.Entity<BudgetProfile>(entity =>
        {
            entity.ToTable("BudgetProfiles");
            entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
            entity.Property(p => p.UserName).HasMaxLength(100);
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.Income).IsRequired();

            // Configure one-to-many relationship
            entity.HasMany(p => p.Categories)
                  .WithOne(c => c.BudgetProfile)
                  .HasForeignKey(c => c.BudgetProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Create index on Name for faster lookups
            entity.HasIndex(p => p.Name);
        });

        modelBuilder.Entity<BudgetCategory>(entity =>
        {
            entity.ToTable("BudgetCategories");
            entity.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.BudgetAmount).IsRequired();

            // Create unique composite index to prevent duplicate categories per profile
            entity.HasIndex(c => new { c.BudgetProfileId, c.CategoryName })
                  .IsUnique()
                  .HasDatabaseName("IX_BudgetCategory_ProfileId_CategoryName");
        });

        modelBuilder.Entity<VendorMapping>(entity =>
        {
            entity.ToTable("VendorMappings");
            entity.Property(v => v.Pattern).HasMaxLength(200).IsRequired();
            entity.Property(v => v.VendorName).HasMaxLength(200).IsRequired();

            // Create index on Pattern for fast lookups
            entity.HasIndex(v => v.Pattern).HasDatabaseName("IX_VendorMapping_Pattern");

            // Configure optional relationship to Category
            entity.HasOne(v => v.Category)
                  .WithMany(c => c.Vendors)
                  .HasForeignKey(v => v.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(500);

            // Create unique index on CategoryName
            entity.HasIndex(c => c.CategoryName)
                  .IsUnique()
                  .HasDatabaseName("IX_Category_CategoryName");
        });
    }

    /// <summary>
    /// Validates that the database connection is working and provides user-friendly error messages.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when connection fails with details about the issue.</exception>
    public async Task ValidateDatabaseConnectionAsync()
    {
        try
        {
            // Try to open a connection and execute a simple query
            await Database.CanConnectAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "XX000" || ex.Message.Contains("Tenant or user not found"))
        {
            throw new InvalidOperationException(
                "\n╔═══════════════════════════════════════════════════════════════════════╗\n" +
                "║                    DATABASE CONNECTION FAILED                         ║\n" +
                "╠═══════════════════════════════════════════════════════════════════════╣\n" +
                "║ Your Supabase project appears to be paused.                           ║\n" +
                "║                                                                       ║\n" +
                "║ Free tier Supabase projects pause after 7 days of inactivity.         ║\n" +
                "║                                                                       ║\n" +
                "║ To fix this:                                                          ║\n" +
                "║   1. Go to https://supabase.com/dashboard                             ║\n" +
                "║   2. Select your project                                              ║\n" +
                "║   3. Click 'Restore project' or 'Unpause'                             ║\n" +
                "║   4. Wait a few moments for the database to become active             ║\n" +
                "║   5. Run this application again                                       ║\n" +
                "╚═══════════════════════════════════════════════════════════════════════╝\n",
                ex);
        }
        catch (NpgsqlException ex)
        {
            throw new InvalidOperationException(
                "\n╔═══════════════════════════════════════════════════════════════════════╗\n" +
                "║                    DATABASE CONNECTION FAILED                         ║\n" +
                "╠═══════════════════════════════════════════════════════════════════════╣\n" +
                $"║ Error: {ex.Message.PadRight(67)}║\n" +
                "║                                                                       ║\n" +
                "║ Possible causes:                                                      ║\n" +
                "║   - Supabase project is paused (check dashboard)                      ║\n" +
                "║   - Network connectivity issues                                       ║\n" +
                "║   - Invalid credentials                                               ║\n" +
                "║   - Database server is down                                           ║\n" +
                "╚═══════════════════════════════════════════════════════════════════════╝\n",
                ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"\n╔═══════════════════════════════════════════════════════════════════════╗\n" +
                "║                    DATABASE CONNECTION FAILED                         ║\n" +
                "╠═══════════════════════════════════════════════════════════════════════╣\n" +
                $"║ Unexpected error: {ex.Message.PadRight(56)}║\n" +
                "╚═══════════════════════════════════════════════════════════════════════╝\n",
                ex);
        }
    }
}
