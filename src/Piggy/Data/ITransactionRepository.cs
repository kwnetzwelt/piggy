using Piggy.Models;

namespace Piggy.Data;

public interface ITransactionRepository
{
    Task<List<Transaction>> GetAllAsync();
    Task<List<Transaction>> GetByVirtualAccountIdAsync(string virtualAccountId);
    Task<List<Transaction>> GetUntaggedAsync();
    Task<Transaction?> GetByExternalIdAsync(string externalId);
    Task CreateAsync(Transaction transaction);
    Task CreateManyAsync(List<Transaction> transactions);
    Task UpdateAsync(Transaction transaction);
    Task<int> GetTotalBalanceForAccountAsync(string virtualAccountId);
    Task<bool> ExistsByExternalIdAsync(string externalId);
    Task<string?> GetSuggestedAccountIdAsync(string description);
    Task DeleteByExternalIdAsync(string externalId);
}
