using Piggy.Models;

namespace Piggy.Data;

public interface IAccountGroupRepository
{
    Task<List<AccountGroup>> GetAllAsync();
    Task<AccountGroup?> GetByIdAsync(string id);
    Task CreateAsync(AccountGroup group);
    Task UpdateAsync(AccountGroup group);
    Task DeleteAsync(string id);
}
