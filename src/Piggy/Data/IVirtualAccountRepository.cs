using Piggy.Models;

namespace Piggy.Data;

public interface IVirtualAccountRepository
{
    Task<List<VirtualAccount>> GetAllAsync();
    Task<VirtualAccount?> GetByIdAsync(string id);
    Task CreateAsync(VirtualAccount account);
    Task UpdateAsync(VirtualAccount account);
    Task DeleteAsync(string id);
    Task<long> CountByGroupAsync(string groupId);
}
