public class Transaction {
    /*
        Transaction Date
        Amount
        Vendor
        Description
    */
    public DateTime Date { get; set; }
    public float Amount { get; set; }
    public string Vendor { get; set; }
    public string Description { get; set; }
}