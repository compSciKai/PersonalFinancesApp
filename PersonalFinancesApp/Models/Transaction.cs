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
    public DateTime Date { get; set; }
    [Name("CAD$")]
    public float Amount { get; set; }

    //public string? Vendor { get; set; }
    [Name("Description 1")]
    public string Description { get; set; }
}