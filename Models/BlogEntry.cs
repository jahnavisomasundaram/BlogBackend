using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BlogAppBackend.Models
{
    public class BlogEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Content { get; set; }

        public string AuthorEmail { get; set; }  // to know who created it

        public string AuthorName { get; set; }

        //public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string ImageUrl { get; set; } = null!;

        public string Visibility { get; set; }


        public DateTime? CreatedAt { get; set; }

        public DateTime? ScheduledPublishDateTime { get; set; }
        public bool Published { get; set; } = false;


        public List<string> Likes { get; set; } = new();
        public List<string> Dislikes { get; set; } = new();

        public List<Comment> Comments { get; set; } = new();
    }

    public class Comment
    {
        public string AuthorEmail { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
