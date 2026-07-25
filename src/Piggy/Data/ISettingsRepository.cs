using Piggy.Models;

namespace Piggy.Data;

public interface ISettingsRepository
{
    Task<Settings> GetAsync();
    Task UpdateAsync(Settings settings);
}
