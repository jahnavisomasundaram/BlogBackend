using System.Text.Json;
using System.Text.Json.Serialization;
using static Supabase.Gotrue.Constants;

namespace BlogAppBackend.Services
{
    public class ApiAuth
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;
        private readonly string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImhkcGd3em93YWxoZGV2c3RpZGZ4Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTEzNDA3OTIsImV4cCI6MjA2NjkxNjc5Mn0.zui0I8znGyr1uFWAquOQT6Bg1Bvg2dezek4gn1L10mQ";

        public ApiAuth(IConfiguration config, HttpClient http)
        {
            _config = config;
            _http = http;
        }

        public async Task<AuthResult> CheckAuthenticationState(string token)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://hdpgwzowalhdevstidfx.supabase.co/auth/v1/user");
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Headers.Add("apikey", SupabaseKey);

                var response = await _http.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {

                    return new AuthResult { status = AuthStatus.Unauthorized };
                }

                var user = JsonSerializer.Deserialize<UserContent>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return new AuthResult
                {
                    status = AuthStatus.Authorized,
                    UserContent = user
                };
            }
            catch (Exception ex)
            {
                // Optionally log exception
                return new AuthResult { status = AuthStatus.Unauthorized };
            }
        }
    }

    public class AuthResult
    {
        public AuthStatus status { get; set; }
        public UserContent UserContent { get; set; }
    }

    public enum AuthStatus
    {
        Authorized,
        Unauthorized
    }

    public class UserContent
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Aud { get; set; }

        public UserMetadata UserMetadata { get; set; }        // Add other fields as needed
    }

    public class UserMetadata
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("full_name")]
        public string Full_Name { get; set; }

        [JsonPropertyName("avatar_url")]
        public string Avatar_Url { get; set; }
    }

    // Optionally, for error handling:
    public class SupabaseError
    {
        public string Message { get; set; }
    }
}




