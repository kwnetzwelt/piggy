using Piggy.Models;

namespace Piggy.Data;

public interface ISplitTemplateRepository
{
    Task<List<SplitTemplate>> GetAllAsync();
    Task<SplitTemplate?> GetByIdAsync(string id);
    Task CreateAsync(SplitTemplate template);
    Task UpdateAsync(SplitTemplate template);
    Task DeleteAsync(string id);
}
