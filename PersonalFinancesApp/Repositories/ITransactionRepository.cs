public interface ITransactionRepository
{
    List<Transaction> GetTransactions(string filePath);
}
