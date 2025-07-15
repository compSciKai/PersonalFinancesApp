using Microsoft.EntityFrameworkCore;
using PersonalFinances.Models;

namespace PersonalFinances.Data;

public class  TransactionContext : DbContext
{
    public DbSet<Transaction> Transactions { get; set; }

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
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasDiscriminator<string>("AccountType")
            .HasValue<RBCTransaction>("RBC")
            .HasValue<AmexTransaction>("Amex")
            .HasValue<PCFinancialTransaction>("PC Financial");

            entity.Property(t => t.Amount)
            .IsRequired();
        });

        modelBuilder.Entity<RBCTransaction>(entity =>
        {
            entity.Property(t => t.Description1)
            .HasMaxLength(100);
            entity.Property(t => t.Description2)
            .HasMaxLength(100);

            // Make Description a computed column that EF ignores for inserts
            entity.Property(t => t.Description)
                .HasComputedColumnSql("CONCAT([Description1], ' ', [Description2])")
                .ValueGeneratedOnAddOrUpdate();
        });
    }
}
