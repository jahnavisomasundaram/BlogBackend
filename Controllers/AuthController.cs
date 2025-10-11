using BlogAppBackend.Attributes;
using BlogAppBackend.Models;
using BlogAppBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SharpCompress.Common;
using Supabase.Gotrue;
using System.Security.Claims;

namespace BlogAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly RegisterServices _registerService;
        private readonly SupabaseService _supabaseService;

        public AuthController(RegisterServices registerService, SupabaseService supabaseService)
        {
            _registerService = registerService;
            _supabaseService = supabaseService;
        }

        //[HttpGet("validate")]
        //[AuthState]
        //public async Task<IActionResult> ValidateTokenAsync()
        //{
        //    var email = HttpContext.Items["Email"]?.ToString();
        //    var name = HttpContext.Items["Name"]?.ToString();

        //    if (string.IsNullOrEmpty(email))
        //        return Unauthorized("Email could not be determined from token.");

        //    if (string.IsNullOrEmpty(name))
        //    {
        //        var user = await _registerService.GetUser(email);
        //        if (user == null)
        //        {
        //            return NotFound("User not found in database.");
        //        }

        //        name = user?.UserName;
        //    }

        //    return Ok(new
        //    {
        //        Email = email,
        //        Name = name 
        //    });
        //}

        [HttpGet("getuser")]
        [AuthState]
        public async Task<IActionResult> GetUser([FromQuery] string email)
        {
            //var email = HttpContext.Items["Email"]?.ToString();
            var user = await _registerService.GetUser(email);
            if (user == null)
            {
                return NotFound("User not found in database.");
            }

            var name = user?.UserName;
            return Ok(name);
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(BlogData data)
        {

            var existing = await _registerService.GetUser(data.Email);
            if (existing != null)
                return Conflict("User already exists.");

            await _registerService.CreateAsyncUser(data);
            return Ok("Registered successfully.");
        }

        [HttpPost("register-google")]
        public async Task<IActionResult> RegisterGoogle(GoogleData data)
        {

            var existing = await _registerService.GetUser(data.Email);
            if (existing != null)
                return Conflict("User already exists.");

            await _registerService.CreateAsyncUserGoogle(data);
            return Ok("Registered successfully.");
        }

        [HttpPost("supabase-signup")]
        public async Task<IActionResult> SupabaseSignUp([FromBody] SupabaseSignUpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and password are required.");

            bool result = await _supabaseService.SignUpUserAsync(request.Email, request.Password);

            if (result)
                return Ok("✅ Supabase user registered successfully.");
            else
                return StatusCode(500, "❌ Failed to sign up on Supabase.");
        }
        public class SupabaseSignUpRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("supabase-login")]
        public async Task<IActionResult> SupabaseLogin([FromBody] SupabaseLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and password are required.");

            var jwt = await _supabaseService.SignInUserAsync(request.Email, request.Password);

            if (!string.IsNullOrEmpty(jwt))
                return Ok(new { token = jwt });
            else
                return Unauthorized("❌ Invalid email or password.");
        }

        public class SupabaseLoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }


        [HttpPost("google-oauth")]
        public async Task<IActionResult> HandleGoogleOAuth([FromBody] GoogleOAuthToken tokenData)
        {
            if (string.IsNullOrWhiteSpace(tokenData.AccessToken))
                return BadRequest("Access token is required.");

            try
            {
                var user = await _supabaseService.GetUserFromAccessToken(tokenData.AccessToken);

                if (user != null)
                {

                    var name = user.UserMetadata.ContainsKey("full_name")
                        ? user.UserMetadata["full_name"]?.ToString()
                        : "Unknown";
                    Console.WriteLine(name);

                    return Ok(new { token = tokenData.AccessToken, Email = user.Email, Id = user.Id, name = name });
                }

                return Unauthorized("User not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error verifying token: " + ex.Message);
            }
        }
        public class GoogleOAuthToken
        {
            public string AccessToken { get; set; }
        }


        [HttpPost("create-blog")]
        [AuthState]
        public async Task<IActionResult> CreateBlog([FromBody] BlogWithImageRequest request)
        {
            var email = HttpContext.Items["Email"]?.ToString();
            var name = HttpContext.Items["Name"]?.ToString();


            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized("Email missing in request context.");

            if (string.IsNullOrEmpty(name))
            {
                var user = await _registerService.GetUser(email);
                if (user == null)
                {
                    return NotFound("User not found in database.");
                }

                name = user?.UserName;
            }

            var blog = new BlogEntry
            {
                Title = request.Title,
                Subtitle = request.Subtitle,
                Content = request.Content,
                AuthorEmail = email,
                AuthorName = name,
                ImageUrl = request.Image,
                Visibility = request.Visibility,
                //CreatedAt = DateTime.Now,
                ScheduledPublishDateTime = request.ScheduledPublishDateTime,
                CreatedAt = null,
                Published = false
            };

            var blogId = await _registerService.CreateBlogAsync(blog);
            await _registerService.AddBlogIdToUserAsync(email, blogId);

            return Ok(new { blogId, message = $"Blog created with ID: {blogId}" });
        }

        [HttpPost("publish")]
        public async Task<IActionResult> PublishBlog([FromBody] string blogId)
        {
            if (string.IsNullOrEmpty(blogId))
                return BadRequest("Blog ID missing.");

            var success = await _registerService.PublishBlogAsync(blogId);

            if (!success)
                return NotFound("No blog found with given ID.");

            return Ok(new { message = "Blog published successfully." });
        }


        public class BlogWithImageRequest
        {
            public string Title { get; set; }
            public string Subtitle { get; set; }
            public string Content { get; set; }
            public string Image { get; set; } = ""; // Supabase URL

            public string Visibility { get; set; }

            public DateTime ScheduledPublishDateTime { get; set; }


        }

        [HttpGet("blogs")]
        public async Task<IActionResult> GetAllBlogs()
        {
            var blogs = await _registerService.DisplayBlog(); // No blogId, so it fetches all
            return Ok(blogs);
        }

        [HttpGet("blog/{id}")]
        public async Task<IActionResult> GetBlogById(string id)
        {
            var blog = await _registerService.GetBlogByIdAsync(id);

            if (blog == null)
                return NotFound();

            return Ok(blog);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload()
        {
            var file = Request.Form.Files.FirstOrDefault();

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var imageUrl = await _supabaseService.UploadImageToStorageAsync(file);

            return imageUrl != null ? Ok(imageUrl) : StatusCode(500, "Upload failed.");
        }

        [HttpGet("getBlogs")]
        [AuthState]
        public async Task<IActionResult> DisplayBlogOfUser([FromQuery] string email)
        {
            //var email = HttpContext.Items["Email"]?.ToString();

            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("User email not found in token.");
            }

            var blogs = await _registerService.GetBlogsByUser(email);

            // ✅ Return empty list instead of 404
            return Ok(blogs ?? new List<BlogEntry>());
        }

        [HttpPut("edit-blog/{id}")]
        [AuthState]
        public async Task<IActionResult> EditBlog(string id, [FromBody] BlogEntry updatedBlog)
        {
            var email = HttpContext.Items["Email"]?.ToString();
            if (string.IsNullOrEmpty(email))
                return Unauthorized("User not authorized.");

            var existingBlog = await _registerService.GetBlogByIdAsync(id);
            if (existingBlog == null)
                return NotFound("Blog not found.");

            if (existingBlog.AuthorEmail != email)
                return Forbid("You can only edit your own blogs.");

            // Check if image has changed → delete old one
            if (!string.IsNullOrWhiteSpace(updatedBlog.ImageUrl) &&
                updatedBlog.ImageUrl != existingBlog.ImageUrl)
            {
                await _supabaseService.DeleteImageFromStorageAsync(existingBlog.ImageUrl);
            }

            updatedBlog.Id = id;
            updatedBlog.AuthorEmail = email;
            updatedBlog.CreatedAt = existingBlog.CreatedAt; // preserve original date

            var success = await _registerService.UpdateBlogAsync(id, updatedBlog);
            return success ? Ok("Blog updated successfully.") : StatusCode(500, "Blog update failed.");
        }


        [HttpDelete("delete-blog/{id}")]
        [AuthState]
        public async Task<IActionResult> DeleteBlog(string id)
        {
            var success = await _registerService.DeleteBlogByIdAsync(id);
            if (!success)
                return NotFound("Blog not found");

            return Ok("✅ Blog deleted successfully");
        }

        [HttpPost("like/{blogId}")]
        [AuthState]
        public async Task<IActionResult> LikeBlog(string blogId)
        {
            var email = HttpContext.Items["Email"]?.ToString();
            return await _registerService.LikeBlog(blogId, email);
        }

        [HttpPost("dislike/{blogId}")]
        [AuthState]
        public async Task<IActionResult> DislikeBlog(string blogId)
        {
            var email = HttpContext.Items["Email"]?.ToString();
            return await _registerService.DislikeBlog(blogId, email);
        }

        [HttpPost("comment/{blogId}")]
        [AuthState]
        public async Task<IActionResult> AddComment(string blogId, [FromBody] string commentText)
        {
            var email = HttpContext.Items["Email"]?.ToString();
            return await _registerService.AddComment(blogId, email, commentText);
        }


        [HttpPost("follow")]
        [AuthState]
        public async Task<IActionResult> ToggleFollow([FromBody] string targetUserEmail)
        {
            try
            {
                var email = HttpContext.Items["Email"]?.ToString();
                if (string.IsNullOrEmpty(email))
                    return Unauthorized("Invalid user token");

                var result = await _registerService.ToggleFollow(email, targetUserEmail);
                if (result == null)
                    return NotFound("User not found");

                return Ok(new { followed = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ToggleFollow: {ex.Message}");
                return StatusCode(500, "Server error occurred");
            }
        }


        [HttpGet("userstats")]
        [AuthState]
        public async Task<IActionResult> GetUserStats([FromQuery] string email)
        {
            //var email = HttpContext.Items["Email"]?.ToString();
            if (string.IsNullOrEmpty(email)) return Unauthorized("Invalid user");

            var stats = await _registerService.GetFollowerStats(email);
            return Ok(stats);
        }

        [HttpGet("isfollowing")]
        [AuthState]
        public async Task<IActionResult> IsFollowing([FromQuery] string targetUserEmail)
        {
            var email = HttpContext.Items["Email"]?.ToString();
            if (string.IsNullOrEmpty(email)) return Unauthorized("Invalid user");

            var user = await _registerService.GetUser(email);
            if (user == null) return NotFound("User not found");

            var targetUser = await _registerService.GetUser(targetUserEmail);
            if (targetUser == null) return NotFound("Target user not found");

            bool isFollowing = user.Following?.Contains(targetUser.Email) ?? false;

            return Ok(new { isFollowing });
        }

        [HttpGet("following")]
        [AuthState]
        public async Task<IActionResult> GetFollowingList()
        {
            var email = HttpContext.Items["Email"]?.ToString();
            if (string.IsNullOrEmpty(email)) return Unauthorized("Invalid user");

            var user = await _registerService.GetUser(email);
            if (user == null) return NotFound("User not found");

            var followingList = user.Following ?? new List<string>();

            return Ok(followingList);
        }

        [HttpGet("get-followers")]
        [AuthState]
        public async Task<IActionResult> GetFollowers([FromQuery] string authorEmail)
        {
            var user = await _registerService.GetUser(authorEmail);
            if (user == null) return NotFound("User not found");

            // Assuming your User model has a Followers property
            var followers = user.Followers ?? new List<string>();

            return Ok(followers);
        }


        [HttpGet("searchusers")]
        public async Task<ActionResult<List<UserSearchResultDto>>> SearchUsers([FromQuery] string query)
        {
            var users = await _registerService.SearchUsersAsync(query);
            return Ok(users);
        }

        
        public class UserSearchResultDto
        {
            public string Name { get; set; }
            public string Email { get; set; }
        }
    }

}


