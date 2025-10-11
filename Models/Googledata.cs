using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BlogAppBackend.Models
{
    public class GoogleData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Email { get; set; }

        public string? UserName { get; set; }

        public List<string> BlogIds { get; set; } = new List<string>();
    }
}
