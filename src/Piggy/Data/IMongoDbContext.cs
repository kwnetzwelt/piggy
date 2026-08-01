using MongoDB.Bson;
using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public interface IMongoDbContext
{
    IMongoCollection<VirtualAccount> VirtualAccounts { get; }
    IMongoCollection<Transaction> Transactions { get; }
    IMongoCollection<SplitTemplate> SplitTemplates { get; }
    IMongoCollection<Settings> Settings { get; }
    IMongoCollection<TOTPSecret> TOTPSecrets { get; }
    IMongoCollection<AccountGroup> AccountGroups { get; }
    Task<string> ExportAsync();
    Task ImportAsync(string json);
}
