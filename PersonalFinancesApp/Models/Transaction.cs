using CsvHelper.Configuration.Attributes;
namespace PersonalFinances.Models;

// TODO: Make transaction abstract class and RBCBanking derived class
public class Transaction {
    [Name("Transaction Date")]
    public DateTime Date { get; set; }
    [Name("CAD$")]
    public float Amount { get; set; }
    [Name("Description 1")]
    public string Description { get; set; }
    public string? Category { get; set; }
    public string? Vendor { get; set; }
}