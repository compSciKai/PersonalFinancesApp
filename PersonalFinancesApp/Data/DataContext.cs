using Microsoft.EntityFrameworkCore;
using PersonalFinances.Models;

namespace PersonalFinances.Data;

public class  TransactionContext : DbContext
{
    public DbSet<RBCTransaction> RBCTransactions { get; set; }
    public DbSet<AmexTransaction> AmexTransactions { get; set; }
    public DbSet<PCFinancialTransaction> PCTransactions { get; set; }
    public DbSet<BudgetProfile> BudgetProfiles { get; set; }
    public DbSet<BudgetCategory> BudgetCategories { get; set; }

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
    }
}
