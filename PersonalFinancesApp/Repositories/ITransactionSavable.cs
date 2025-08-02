using PersonalFinances.Models;

namespace PersonalFinances.Repositories
{
    public interface ITransactionSavable
    {
        public Task<int> SaveTransactionsWithHashAsync<T>(List<T> transactions) where T : Transaction;

    }
}