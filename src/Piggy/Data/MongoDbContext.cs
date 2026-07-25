using MongoDB.Bson;
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

    public IMongoCollection<TOTPSecret> TOTPSecrets =>
        _database.GetCollection<TOTPSecret>("totpSecrets");

    public async Task<string> ExportAsync()
    {
        var virtualAccounts = await VirtualAccounts.Find(new BsonDocument()).ToListAsync();
        var transactions = await Transactions.Find(new BsonDocument()).ToListAsync();
        var splitTemplates = await SplitTemplates.Find(new BsonDocument()).ToListAsync();
        var settings = await Settings.Find(new BsonDocument()).ToListAsync();
        var totpSecrets = await TOTPSecrets.Find(new BsonDocument()).ToListAsync();

        var doc = new BsonDocument
        {
            ["virtualAccounts"] = new BsonArray(virtualAccounts.Select(v => v.ToBsonDocument())),
            ["transactions"] = new BsonArray(transactions.Select(t => t.ToBsonDocument())),
            ["splitTemplates"] = new BsonArray(splitTemplates.Select(s => s.ToBsonDocument())),
            ["settings"] = new BsonArray(settings.Select(s => s.ToBsonDocument())),
            ["totpSecrets"] = new BsonArray(totpSecrets.Select(s => s.ToBsonDocument()))
        };

        return doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true });
    }

    public async Task ImportAsync(string json)
    {
        var doc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);

        var collections = new Dictionary<string, IMongoCollection<BsonDocument>>
        {
            ["virtualAccounts"] = _database.GetCollection<BsonDocument>("virtualAccounts"),
            ["transactions"] = _database.GetCollection<BsonDocument>("transactions"),
            ["splitTemplates"] = _database.GetCollection<BsonDocument>("splitTemplates"),
            ["settings"] = _database.GetCollection<BsonDocument>("settings"),
            ["totpSecrets"] = _database.GetCollection<BsonDocument>("totpSecrets")
        };

        foreach (var (key, collection) in collections)
        {
            _database.DropCollection(key);
            if (doc.TryGetValue(key, out var value) && value.IsBsonArray)
            {
                var documents = value.AsBsonArray.Select(v => v.AsBsonDocument).ToList();
                if (documents.Count > 0)
                    await collection.InsertManyAsync(documents);
            }
        }
    }
}
