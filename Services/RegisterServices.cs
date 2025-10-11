using BlogAppBackend.Controllers;
using BlogAppBackend.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Supabase.Gotrue;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Models;
using static System.Reflection.Metadata.BlobBuilder;
using Supabase.Postgrest.Attributes;

namespace BlogAppBackend.Services
{

    public class RegisterServices
    {
        private readonly IMongoCollection<BlogData> _loginCollection;
        private readonly IMongoCollection<GoogleData> _googleCollection;
        private readonly IMongoCollection<BlogEntry> _blogCollection;

        private readonly SupabaseService _supabaseService;

        public RegisterServices(IConfiguration config, SupabaseService supabaseService)
        {
            var client = new MongoClient(config.GetConnectionString("MongoDb"));
            var database = client.GetDatabase("Blogdb");
            _loginCollection = database.GetCollection<BlogData>("BlogData");
            _googleCollection = database.GetCollection<GoogleData>("BlogData");
            _blogCollection = database.GetCollection<BlogEntry>("Blogs");
            _supabaseService = supabaseService;
        }

        public async Task CreateAsyncUser(BlogData userData) =>
        await _loginCollection.InsertOneAsync(userData);

        public async Task CreateAsyncUserGoogle(GoogleData userData) =>
            await _googleCollection.InsertOneAsync(userData);
        public async Task<BlogData> GetUser(string email) =>
            await _loginCollection.Find(w => w.Email == email).FirstOrDefaultAsync();

        public async Task<List<UserSearchResultDto>> SearchUsersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<UserSearchResultDto>();

            // Case-insensitive filter on UserName or Email
            var filter = Builders<BlogData>.Filter.Or(
                Builders<BlogData>.Filter.Regex(u => u.UserName, new MongoDB.Bson.BsonRegularExpression(query, "i")),
                Builders<BlogData>.Filter.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(query, "i"))
            );

            var projection = Builders<BlogData>.Projection.Include(u => u.UserName).Include(u => u.Email);

            var users = await _loginCollection.Find(filter)
                .Project<BlogData>(projection)
                .Limit(20)
                .ToListAsync();

            return users.Select(u => new UserSearchResultDto
            {
                UserName = u.UserName,
                Email = u.Email
            }).ToList();
        }

        public class UserSearchResultDto
        {
            public string UserName { get; set; } // corresponds to user.UserName in BlogData
            public string Email { get; set; }
        }

        [Table("blog_events")]
        public class BlogEvent : BaseModel
        {
            [Column("id")]
            public string Id { get; set; } = Guid.NewGuid().ToString();

            [Column("blog_id")]
            public string BlogId { get; set; }

            [Column("author_id")]
            public string AuthorId { get; set; }

            [Column("action_id")]
            public string ActionId { get; set; }

            [Column("target_email")]
            public string TargetEmail { get; set; }

            [Column("timestamp")]
            public DateTime? Timestamp { get; set; } = DateTime.Now;
        }



        //public async Task<string> CreateBlogAsync(BlogEntry entry)
        //{
        //    try
        //    {
        //        // 1️⃣ Insert the blog entry into MongoDB
        //        await _blogCollection.InsertOneAsync(entry);
        //        Console.WriteLine($"✅ Blog inserted into MongoDB with ID: {entry.Id}");

        //        // 2️⃣ Get follower emails
        //        var followers = await GetFollowerEmailsAsync(entry.AuthorEmail);
        //        Console.WriteLine("✅ Followers fetched: " + (followers.Any() ? string.Join(", ", followers) : "No followers"));

        //        // 3️⃣ Initialize Supabase client
        //        var client = await _supabaseService.GetSupabaseClientAsync();
        //        Console.WriteLine("✅ Supabase client initialized");

        //        // 4️⃣ Prepare events
        //        var blogEvents = followers.Select(followerEmail => new BlogEvent
        //        {
        //            BlogId = entry.Id,
        //            AuthorId = entry.AuthorEmail,
        //            TargetEmail = followerEmail,
        //            ActionId = "create"
        //        }).ToList();

        //        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(blogEvents));
        //        if (blogEvents.Count > 0)
        //        {
        //            Console.WriteLine($"📦 Prepared {blogEvents.Count} blog events for insert:");
        //            foreach (var evt in blogEvents)
        //            {
        //                Console.WriteLine($"  blog_id={evt.BlogId}, author_id={evt.AuthorId}, target_email={evt.TargetEmail}, action_id={evt.ActionId}");
        //            }

        //            try
        //            {

        //                var response = await client.From<BlogEvent>().Insert(blogEvents);

        //                Console.WriteLine($"📄 Supabase HTTP Status: {response.ResponseMessage.StatusCode}");

        //                if (!response.ResponseMessage.IsSuccessStatusCode)
        //                {
        //                    throw new Exception($"Supabase insert failed.\nStatus: {response.ResponseMessage.StatusCode}");
        //                }

        //                Console.WriteLine("✅ Blog events inserted into Supabase successfully");
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"🔥 Supabase Insert Exception: {ex}");
        //                throw;
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine("ℹ️ No followers to notify, skipping Supabase insert");
        //        }



        //        return entry.Id;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"🔥 Error in CreateBlogAsync: {ex.Message}");
        //        Console.WriteLine($"📄 Stack Trace: {ex.StackTrace}");
        //        throw; // rethrow so ASP.NET can return a 500 with logs
        //    }
        //}

        public async Task<string> CreateBlogAsync(BlogEntry entry)
        {
            try
            {
                // 1️⃣ Insert the blog entry into MongoDB
                await _blogCollection.InsertOneAsync(entry);
                Console.WriteLine($"✅ Blog inserted into MongoDB with ID: {entry.Id}");

                // 2️⃣ Get follower emails
                var followers = await GetFollowerEmailsAsync(entry.AuthorEmail);
                Console.WriteLine("✅ Followers fetched: " + (followers.Any() ? string.Join(", ", followers) : "No followers"));

                // 3️⃣ Initialize Supabase client
                var client = await _supabaseService.GetSupabaseClientAsync();
                Console.WriteLine("✅ Supabase client initialized");

                // 4️⃣ Prepare follower events for Supabase
                var blogEvents = followers.Select(followerEmail => new BlogEvent
                {
                    BlogId = entry.Id,
                    AuthorId = entry.AuthorEmail,
                    TargetEmail = followerEmail,
                    ActionId = "create"
                }).ToList();

                if (blogEvents.Count > 0)
                {
                    try
                    {
                        var response = await client.From<BlogEvent>().Insert(blogEvents);

                        Console.WriteLine($"📄 Supabase HTTP Status: {response.ResponseMessage.StatusCode}");

                        if (!response.ResponseMessage.IsSuccessStatusCode)
                        {
                            throw new Exception($"Supabase insert failed.\nStatus: {response.ResponseMessage.StatusCode}");
                        }

                        Console.WriteLine("✅ Blog events inserted into Supabase successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"🔥 Supabase Insert Exception: {ex}");
                        throw;
                    }
                }
                else
                {
                    Console.WriteLine("ℹ️ No followers to notify, skipping Supabase insert");
                }

                // 5️⃣ Add to Supabase scheduled_blogs if blog is scheduled for future
                if (entry.ScheduledPublishDateTime.HasValue && entry.ScheduledPublishDateTime > DateTime.UtcNow)
                {
                    try
                    {
                        var scheduledBlog = new ScheduledBlog
                        {
                            BlogId = entry.Id.ToString(),
                            //AuthorEmail = entry.AuthorEmail,
                            ScheduledTime = entry.ScheduledPublishDateTime.Value.ToUniversalTime(),
                            Executed = false
                        };

                        var scheduleResponse = await client.From<ScheduledBlog>().Insert(scheduledBlog);

                        if (!scheduleResponse.ResponseMessage.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"⚠️ Failed to insert schedule record: {scheduleResponse.ResponseMessage.StatusCode}");
                        }
                        else
                        {
                            Console.WriteLine("✅ Scheduled blog added to Supabase table");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"🔥 Error adding schedule to Supabase: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("🕒 Blog not scheduled for future — skipping scheduling step");
                }

                return entry.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Error creating blog: {ex.Message}");
                throw;
            }
        }


        //public class BlogEvent : BaseModel
        //{
        //    public string blog_id { get; set; }
        //    public string author_id { get; set; }
        //    public string action_id { get; set; }
        //}

        public async Task AddBlogIdToUserAsync(string email, string blogId)
        {
            var filter = Builders<BlogData>.Filter.Eq(u => u.Email, email);
            var update = Builders<BlogData>.Update.Push(u => u.BlogIds, blogId);

            await _loginCollection.UpdateOneAsync(filter, update);
        }

        public async Task<bool> PublishBlogAsync(string blogId)
        {
            if (string.IsNullOrEmpty(blogId))
                throw new ArgumentException("Blog ID cannot be null or empty.", nameof(blogId));

            var filter = Builders<BlogEntry>.Filter.Eq(b => b.Id, blogId);
            var update = Builders<BlogEntry>.Update
                .Set(b => b.Published, true)
                .Set(b => b.CreatedAt, DateTime.UtcNow);

            var result = await _blogCollection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
                return false; // No blog found

            Console.WriteLine($"✅ Blog {blogId} published successfully.");
            return true;
        }


        public async Task<List<BlogEntry>> DisplayBlog(string blogId = null)
        {
            FilterDefinition<BlogEntry> filter = string.IsNullOrEmpty(blogId)
                ? Builders<BlogEntry>.Filter.Empty
                : Builders<BlogEntry>.Filter.Eq(b => b.Id, blogId);

            return await _blogCollection.Find(filter).ToListAsync();
        }

        public async Task<BlogEntry> GetBlogByIdAsync(string id)
        {
            var filter = Builders<BlogEntry>.Filter.Eq("_id", ObjectId.Parse(id));
            return await _blogCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<BlogEntry>> GetBlogsByUser(string email)
        {
            var filter = Builders<BlogEntry>.Filter.Eq(b => b.AuthorEmail, email);
            var blogs = await _blogCollection.Find(filter).ToListAsync();
            return blogs;
        }

        public async Task<bool> DeleteBlogByIdAsync(string blogId)
        {
            var blog = await _blogCollection.Find(b => b.Id == blogId).FirstOrDefaultAsync();
            if (blog == null)
                return false;

            if (!string.IsNullOrEmpty(blog.ImageUrl))
            {
                var deleted = await _supabaseService.DeleteImageFromStorageAsync(blog.ImageUrl);
                if (!deleted)
                {
                    Console.WriteLine("⚠️ Image deletion failed or image not found in storage.");
                }
            }

            // Delete blog from blog collection
            await _blogCollection.DeleteOneAsync(b => b.Id == blogId);

            // Remove blogId from the user who created it
            var filter = Builders<BlogData>.Filter.Eq(u => u.Email, blog.AuthorEmail);
            var update = Builders<BlogData>.Update.Pull(u => u.BlogIds, blogId);
            await _loginCollection.UpdateOneAsync(filter, update);

            return true;
        }

        public async Task<bool> UpdateBlogAsync(string blogId, BlogEntry updatedBlog)
        {
            var filter = Builders<BlogEntry>.Filter.Eq(b => b.Id, blogId);

            var update = Builders<BlogEntry>.Update
                .Set(b => b.Title, updatedBlog.Title)
                .Set(b => b.Subtitle, updatedBlog.Subtitle)
                .Set(b => b.Content, updatedBlog.Content)
                .Set(b => b.ImageUrl, updatedBlog.ImageUrl) // optional
                .Set(b => b.Visibility, updatedBlog.Visibility);

            var result = await _blogCollection.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

        public async Task<IActionResult> LikeBlog(string blogId, string userEmail)
        {
            var filter = Builders<BlogEntry>.Filter.Eq(x => x.Id, blogId);
            var blog = await _blogCollection.Find(filter).FirstOrDefaultAsync();

            if (blog == null)
                return new NotFoundResult();

            if (blog.Likes.Contains(userEmail))
            {
                blog.Likes.Remove(userEmail); // UNLIKE if already liked
            }
            else
            {
                blog.Likes.Add(userEmail);      // Add like
                blog.Dislikes.Remove(userEmail); // Remove dislike if any
            }

            var update = Builders<BlogEntry>.Update
                .Set(b => b.Likes, blog.Likes)
                .Set(b => b.Dislikes, blog.Dislikes);

            await _blogCollection.UpdateOneAsync(filter, update);
            return new OkResult();
        }

        public async Task<IActionResult> DislikeBlog(string blogId, string userEmail)
        {
            var filter = Builders<BlogEntry>.Filter.Eq(x => x.Id, blogId);
            var blog = await _blogCollection.Find(filter).FirstOrDefaultAsync();

            if (blog == null)
                return new NotFoundResult();

            if (blog.Dislikes.Contains(userEmail))
            {
                blog.Dislikes.Remove(userEmail); // UNDISLIKE if already disliked
            }
            else
            {
                blog.Dislikes.Add(userEmail);    // Add dislike
                blog.Likes.Remove(userEmail);    // Remove like if any
            }

            var update = Builders<BlogEntry>.Update
                .Set(b => b.Likes, blog.Likes)
                .Set(b => b.Dislikes, blog.Dislikes);

            await _blogCollection.UpdateOneAsync(filter, update);
            return new OkResult();
        }

        public async Task<IActionResult> AddComment(string blogId, string userEmail, string commentText)
        {
            var filter = Builders<BlogEntry>.Filter.Eq(x => x.Id, blogId);
            var update = Builders<BlogEntry>.Update.Push(x => x.Comments, new Comment
            {
                AuthorEmail = userEmail,
                Text = commentText,
                Timestamp = DateTime.UtcNow
            });

            var result = await _blogCollection.UpdateOneAsync(filter, update);
            if (result.MatchedCount == 0)
                return new NotFoundResult();

            return new OkResult();
        }


        public async Task<bool?> ToggleFollow(string currentUserEmail, string targetUserEmail)
        {
            var currentUser = await _loginCollection.Find(u => u.Email == currentUserEmail).FirstOrDefaultAsync();
            var targetUser = await _loginCollection.Find(u => u.Email == targetUserEmail).FirstOrDefaultAsync();

            if (currentUser == null || targetUser == null || currentUser.Email == targetUser.Email)
                return null;

            bool isNowFollowing;

            if (currentUser.Following.Contains(targetUserEmail))
            {
                // Unfollow
                currentUser.Following.Remove(targetUserEmail);
                targetUser.Followers.Remove(currentUserEmail);
                isNowFollowing = false;
            }
            else
            {
                // Follow
                currentUser.Following.Add(targetUserEmail);
                targetUser.Followers.Add(currentUserEmail);
                isNowFollowing = true;
            }

            // Update both users in MongoDB
            var updateCurrent = Builders<BlogData>.Update.Set(u => u.Following, currentUser.Following);
            var updateTarget = Builders<BlogData>.Update.Set(u => u.Followers, targetUser.Followers);

            await _loginCollection.UpdateOneAsync(u => u.Id == currentUser.Id, updateCurrent);
            await _loginCollection.UpdateOneAsync(u => u.Id == targetUser.Id, updateTarget);

            return isNowFollowing;
        }


        public async Task<object> GetFollowerStats(string email)
        {
            var user = await _loginCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null) return null;

            var followerEmails = user.Followers ?? new List<string>();
            var followingEmails = user.Following ?? new List<string>();

            // Fetch follower user names
            var followerUsers = await _loginCollection.Find(u => followerEmails.Contains(u.Email))
                                                       .Project(u => new { u.UserName })
                                                       .ToListAsync();
            var followingUsers = await _loginCollection.Find(u => followingEmails.Contains(u.Email))
                                                        .Project(u => new { u.UserName })
                                                        .ToListAsync();

            return new
            {
                followers = followerUsers.Select(u => u.UserName).ToList(),
                following = followingUsers.Select(u => u.UserName).ToList(),
                followerCount = followerUsers.Count,
                followingCount = followingUsers.Count
            };
        }



        public async Task<List<string>> GetFollowerEmailsAsync(string userEmail)
        {
            var user = await _loginCollection.Find(u => u.Email == userEmail).FirstOrDefaultAsync();

            if (user == null || user.Followers == null)
                return new List<string>();

            return user.Followers;
        }



        //public async Task UpdateBlogForUserAsync(string email, BlogData blog)
        //{
        //    var filter = Builders<BlogData>.Filter.Eq(x => x.Email, email);
        //    var update = Builders<BlogData>.Update
        //        .Set(x => x.Title, blog.Title)
        //        .Set(x => x.Subtitle, blog.Subtitle)
        //        .Set(x => x.Content, blog.Content);

        //    await _loginCollection.UpdateOneAsync(filter, update);
        //}
    }
}


