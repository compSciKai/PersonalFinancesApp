namespace PersonalFinances.Models;

public class CsvImportSettings
{
    public CsvTransactionTypeSettings? RBC { get; set; }
    public CsvTransactionTypeSettings? Amex { get; set; }
    public CsvTransactionTypeSettings? PCFinancial { get; set; }
}

public class CsvTransactionTypeSettings
{
    public string? InputFolder { get; set; }
    public string? ProcessedFolder { get; set; }
}
