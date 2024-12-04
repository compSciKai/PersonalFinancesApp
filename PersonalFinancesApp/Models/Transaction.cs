using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;
namespace PersonalFinances.Models;

// TODO: Make transaction abstract class and RBCBanking derived class
public class Transaction {
    [Key]
    public int Id { get; set; }
    [Name("Account Type")]
    public string AccountType { get; set; }
    [Name("Account Number")]
    public string AccountNumber { get; set; }
    [Name("Transaction Date")]
    public DateTime Date { get; set; }
    [Name("CAD$")]
    public decimal Amount { get; set; }    
    [Name("Description 1")]
    public string Description1 { get; set; }
    [Name("Description 2")]
    public string? Description2 { get; set; }
    public string? Category { get; set; }
    public string? Vendor { get; set; }

    public string Description {
        get { return new String(Description1 + " " + Description2).TrimEnd(); }
    }
}