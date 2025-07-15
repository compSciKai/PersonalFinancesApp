using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;
namespace PersonalFinances.Models;


public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
}

public abstract class Transaction : BaseEntity
{
    public string? Category { get; set; }
    public string? Vendor { get; set; }
    public virtual DateTime Date { get; set; }
    public virtual string Description { get; set; }
    [Required]
    public virtual decimal Amount { get; set; }
    public virtual string AccountType { get; set; }
    public virtual string MemberName { get; set; } = string.Empty;
    public virtual bool isNegativeAmounts { get; } = true;
}

public class RBCTransaction : Transaction {
    [Name("Account Type")]
    public override string AccountType { get; set; } = "RBC";
    [Name("Transaction Date")]
    public override DateTime Date { get; set; }
    [Name("CAD$")]
    public override decimal Amount { get; set; }
    [Name("Description 1")]
    public string Description1 { get; set; }
    [Name("Description 2")]
    public string Description2 { get; set; }

    public override string Description
    {
        get
        {
            return $"{Description1} {Description2}".Trim();
        }
    }
}

public class AmexTransaction : Transaction
{
    [Name("Date")]
    public override DateTime Date {  get; set; }
    [Name("Date Processed")]
    public DateTime ProcessedDate { get; set; }
    [Name("Amount")]
    public override decimal Amount { get; set; }
    [Name("Description")]
    public override string Description { get; set; }
    [Name("Account #")]
    public string AccountNumber { get; set; }
    [Name("Card Member")]
    public override string MemberName { get; set; }
    public override string AccountType { get; set; } = "Amex";
    public override bool isNegativeAmounts { get; } = false;
}

public class PCFinancialTransaction : Transaction
{
    [Name("Date")]
    public override DateTime Date { get; set; }
    [Name("Description")]
    public override string Description { get; set; }
    [Name("Amount")]
    public override decimal Amount { get; set; }
    [Name("Card Holder Name")]
    public override string MemberName { get; set; }
    [Name("Type")]
    public string TransactionType { get; set; }
    public override string AccountType { get; set; } = "PC Financial";
}