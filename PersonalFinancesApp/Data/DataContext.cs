using Microsoft.EntityFrameworkCore;
using PersonalFinances.Models;

namespace PersonalFinances.Data;

public class TransactionContext : DbContext
{
    public DbSet<Transaction> TransactionItem { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlServer(
                @"Server=localhost;Database=FinancialStatements;Trusted_Connection=True;TrustServerCertificate=True",
                options => options.EnableRetryOnFailure());
        }

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("PersonalFinancesAppSchema");
        modelBuilder.Entity<Transaction>()
            .Property(e => e.Id)
            .ValueGeneratedOnAdd();
    }
}
