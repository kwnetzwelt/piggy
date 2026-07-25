using Piggy.Models;

namespace Piggy.Data;

public interface ITOTPSecretRepository
{
    Task<TOTPSecret?> GetByUserIdAsync(string userId);
    Task CreateOrUpdateAsync(TOTPSecret secret);
    Task DeleteAsync(string userId);
}
