using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public class SplitTemplateRepository : ISplitTemplateRepository
{
    private readonly IMongoDbContext _context;

    public SplitTemplateRepository(IMongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<SplitTemplate>> GetAllAsync()
    {
        return await _context.SplitTemplates
            .Find(_ => true)
            .SortBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<SplitTemplate?> GetByIdAsync(string id)
    {
        return await _context.SplitTemplates
            .Find(t => t.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(SplitTemplate template)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SplitTemplates.InsertOneAsync(template);
    }

    public async Task UpdateAsync(SplitTemplate template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        await _context.SplitTemplates.ReplaceOneAsync(t => t.Id == template.Id, template);
    }

    public async Task DeleteAsync(string id)
    {
        await _context.SplitTemplates.DeleteOneAsync(t => t.Id == id);
    }
}
