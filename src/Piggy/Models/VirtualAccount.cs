using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Piggy.Models;

public class VirtualAccount
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("color")]
    public string Color { get; set; } = "#6c757d";

    [BsonElement("startingBalance")]
    public decimal StartingBalance { get; set; }

    [BsonElement("matchStrings")]
    [BsonIgnoreIfNull]
    public List<string>? MatchStrings { get; set; }

    [BsonElement("groupId")]
    [BsonIgnoreIfNull]
    public string? GroupId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
