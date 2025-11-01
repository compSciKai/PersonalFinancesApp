using Microsoft.EntityFrameworkCore;
using PersonalFinances.Models;

namespace PersonalFinances.Data;

public class  TransactionContext : DbContext
{
    public DbSet<RBCTransaction> RBCTransactions { get; set; }
    public DbSet<AmexTransaction> AmexTransactions { get; set; }
    public DbSet<PCFinancialTransaction> PCTransactions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlServer(
                @"",
                options => options.EnableRetryOnFailure());
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
    }
}
