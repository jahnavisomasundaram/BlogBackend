using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BlogAppBackend.Services;
using static Supabase.Gotrue.Constants;

namespace BlogAppBackend.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthStateAttribute : Attribute, IAsyncActionFilter
    {

        private readonly bool _checkCourseAccess;
        public AuthStateAttribute(bool checkCourseAccess = false)
        {
            _checkCourseAccess = checkCourseAccess;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var authService = context.HttpContext.RequestServices.GetRequiredService<ApiAuth>();
            //var dataService = context.HttpContext.RequestServices.GetRequiredService<DataService>();

            if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                context.Result = new UnauthorizedResult();
                return;
            }


            var token = authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader.ToString().Substring(7).Trim()
                : authHeader.ToString().Trim();


            var AuthData = await authService.CheckAuthenticationState(token);
            Console.WriteLine($"🔐 Validating token: {token}");


            if (AuthData.status == AuthStatus.Unauthorized)
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            else
            {
                context.HttpContext.Items["Email"] = AuthData.UserContent.Email;
                var name = "";

                if (AuthData.UserContent.UserMetadata != null)
                {
                    if (!string.IsNullOrEmpty(AuthData.UserContent.UserMetadata.Full_Name))
                    {
                        name = AuthData.UserContent.UserMetadata.Full_Name;
                    }
                    else if (!string.IsNullOrEmpty(AuthData.UserContent.UserMetadata.Name))
                    {
                        name = AuthData.UserContent.UserMetadata.Name;
                    }
                }

                context.HttpContext.Items["Name"] = name;
            }

            await next();
        }
    }
}
