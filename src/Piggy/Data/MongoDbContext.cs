using MongoDB.Driver;
using Piggy.Models;

namespace Piggy.Data;

public class MongoDbContext : IMongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, string databaseName)
    {
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<VirtualAccount> VirtualAccounts =>
        _database.GetCollection<VirtualAccount>("virtualAccounts");

    public IMongoCollection<Transaction> Transactions =>
        _database.GetCollection<Transaction>("transactions");

    public IMongoCollection<SplitTemplate> SplitTemplates =>
        _database.GetCollection<SplitTemplate>("splitTemplates");

    public IMongoCollection<Settings> Settings =>
        _database.GetCollection<Settings>("settings");
}
