using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Piggy.Models;

public class Transaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("externalId")]
    public string ExternalId { get; set; } = string.Empty;

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "EUR";

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("spaceId")]
    public string SpaceId { get; set; } = string.Empty;

    [BsonElement("spaceName")]
    public string SpaceName { get; set; } = string.Empty;

    [BsonElement("virtualAccountId")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? VirtualAccountId { get; set; }

    [BsonElement("importedAt")]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isProcessed")]
    public bool IsProcessed { get; set; }

    [BsonElement("isOverflow")]
    public bool IsOverflow { get; set; }

    [BsonElement("splitFrom")]
    [BsonIgnoreIfNull]
    public string? SplitFrom { get; set; }

    [BsonElement("splitTemplateId")]
    [BsonIgnoreIfNull]
    public string? SplitTemplateId { get; set; }

    [BsonElement("splitItems")]
    [BsonIgnoreIfNull]
    public List<SplitTemplateItem>? SplitItems { get; set; }
}
