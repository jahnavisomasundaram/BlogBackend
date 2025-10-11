//using BlogAppBackend.Attributes;
//using BlogAppBackend.Models;
//using BlogAppBackend.Services;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.SignalR;

//namespace BlogAppBackend.Controllers
//{
//    [ApiController]
//    [Route("[controller]")]
//    public class BlogController : ControllerBase
//    {
//        private readonly IHubContext<NotificationHub> _hubContext;
//        private readonly RegisterServices _registerService;

//        public BlogController(IHubContext<NotificationHub> hubContext, RegisterServices registerService)
//        {
//            _hubContext = hubContext;
//            _registerService = registerService;
//        }

//        [HttpPost("postblog")]
//        public async Task<IActionResult> PostBlog([FromBody] BlogEntry blog)
//        {
//            // Save the blog...

//            // Get followers of the user
//            var followers = await _registerService.GetFollowerEmailsAsync(blog.AuthorEmail);

//            foreach (var followerEmail in followers)
//            {
//                await _hubContext.Clients.User(followerEmail)
//                    .SendAsync("ReceiveNotification", $"{blog.AuthorName} posted: {blog.Title}");
//            }

//            return Ok();

//        }
//    }
//}
