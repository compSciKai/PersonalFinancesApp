using CsvHelper.Configuration.Attributes;

namespace PersonalFinances.Models;
public class Transaction {
    /*
        Transaction Date
        Amount
        Vendor
        Description
    */
    [Name("Transaction Date")]
    public DateOnly Date { get; set; }
    [Name("CAD$")]
    public float Amount { get; set; }
    [Name("Description 1")]
    public string Description { get; set; }
    public string? Category { get; set; }
    public string? Vendor { get; set; }
}