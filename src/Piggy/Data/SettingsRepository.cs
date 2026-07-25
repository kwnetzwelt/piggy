using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public class SettingsRepository : ISettingsRepository
{
    private readonly IMongoCollection<Settings> _settings;

    public SettingsRepository(IMongoDbContext context)
    {
        _settings = context.Settings;
    }

    public async Task<Settings> GetAsync()
    {
        var settings = await _settings.Find(_ => true).FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new Settings { Id = "singleton" };
            await _settings.InsertOneAsync(settings);
        }
        return settings;
    }

    public async Task UpdateAsync(Settings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        await _settings.ReplaceOneAsync(
            s => s.Id == settings.Id,
            settings,
            new ReplaceOptions { IsUpsert = true });
    }
}
