using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public class VirtualAccountRepository : IVirtualAccountRepository
{
    private readonly IMongoDbContext _context;

    public VirtualAccountRepository(IMongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<VirtualAccount>> GetAllAsync()
    {
        return await _context.VirtualAccounts.Find(_ => true).ToListAsync();
    }

    public async Task<VirtualAccount?> GetByIdAsync(string id)
    {
        return await _context.VirtualAccounts.Find(a => a.Id == id).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(VirtualAccount account)
    {
        account.CreatedAt = DateTime.UtcNow;
        account.UpdatedAt = DateTime.UtcNow;
        await _context.VirtualAccounts.InsertOneAsync(account);
    }

    public async Task UpdateAsync(VirtualAccount account)
    {
        account.UpdatedAt = DateTime.UtcNow;
        await _context.VirtualAccounts.ReplaceOneAsync(a => a.Id == account.Id, account);
    }

    public async Task DeleteAsync(string id)
    {
        await _context.VirtualAccounts.DeleteOneAsync(a => a.Id == id);
    }
}
