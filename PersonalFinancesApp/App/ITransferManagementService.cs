using PersonalFinances.Models;

namespace PersonalFinances.App;

public interface ITransferManagementService
{
    Task ManageTransfersAsync(List<Transaction> transactions, BudgetProfile profile);
}
