using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public class AccountGroupRepository : IAccountGroupRepository
{
    private readonly IMongoDbContext _context;

    public AccountGroupRepository(IMongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<AccountGroup>> GetAllAsync()
    {
        return await _context.AccountGroups.Find(_ => true).ToListAsync();
    }

    public async Task<AccountGroup?> GetByIdAsync(string id)
    {
        return await _context.AccountGroups.Find(g => g.Id == id).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(AccountGroup group)
    {
        group.CreatedAt = DateTime.UtcNow;
        group.UpdatedAt = DateTime.UtcNow;
        await _context.AccountGroups.InsertOneAsync(group);
    }

    public async Task UpdateAsync(AccountGroup group)
    {
        group.UpdatedAt = DateTime.UtcNow;
        await _context.AccountGroups.ReplaceOneAsync(g => g.Id == group.Id, group);
    }

    public async Task DeleteAsync(string id)
    {
        await _context.AccountGroups.DeleteOneAsync(g => g.Id == id);
    }
}
