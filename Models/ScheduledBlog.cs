using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

[Table("scheduled_blogs")]
public class ScheduledBlog : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("blog_id")]
    public string BlogId { get; set; }

    //[Column("author_email")]
    //public string AuthorEmail { get; set; }

    [Column("scheduled_time")]
    public DateTime ScheduledTime { get; set; }

    [Column("executed")]
    public bool Executed { get; set; } = false;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
}
