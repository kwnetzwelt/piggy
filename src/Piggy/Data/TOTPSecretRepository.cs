using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public class TOTPSecretRepository : ITOTPSecretRepository
{
    private readonly IMongoDbContext _context;

    public TOTPSecretRepository(IMongoDbContext context)
    {
        _context = context;
    }

    public async Task<TOTPSecret?> GetByUserIdAsync(string userId)
    {
        return await _context.TOTPSecrets
            .Find(s => s.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task CreateOrUpdateAsync(TOTPSecret secret)
    {
        await _context.TOTPSecrets.ReplaceOneAsync(
            s => s.UserId == secret.UserId,
            secret,
            new ReplaceOptions { IsUpsert = true });
    }

    public async Task DeleteAsync(string userId)
    {
        await _context.TOTPSecrets.DeleteOneAsync(s => s.UserId == userId);
    }
}
