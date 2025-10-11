using Supabase;
using Supabase.Gotrue;
using System;
using System.Threading.Tasks;
namespace BlogAppBackend.Services
{
    public class SupabaseService
    {
        public Supabase.Client SupabaseClient { get; private set; }

        private readonly string SupabaseUrl = "https://hdpgwzowalhdevstidfx.supabase.co";
        private readonly string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImhkcGd3em93YWxoZGV2c3RpZGZ4Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTEzNDA3OTIsImV4cCI6MjA2NjkxNjc5Mn0.zui0I8znGyr1uFWAquOQT6Bg1Bvg2dezek4gn1L10mQ";
        private bool _initialized = false;
        public SupabaseService()
        {
            var options = new SupabaseOptions { AutoConnectRealtime = false };
            SupabaseClient = new Supabase.Client(SupabaseUrl, SupabaseKey, options);
        }

        public async Task InitializeAsync()
        {
            if (!_initialized)
            {
                await SupabaseClient.InitializeAsync();
                _initialized = true;
            }
        }

        public async Task<bool> SignUpUserAsync(string email, string password)
        {
            try
            {
                await InitializeAsync();

                var response = await SupabaseClient.Auth.SignUp(email, password);

                if (response?.User != null)
                {
                    Console.WriteLine("✅ User registered successfully.");
                    return true;
                }

                Console.WriteLine("❌ Registration failed, no user returned.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Exception during signup: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> SignInUserAsync(string email, string password)
        {
            try
            {
                await InitializeAsync();

                var session = await SupabaseClient.Auth.SignIn(email, password);

                if (session != null && !string.IsNullOrWhiteSpace(session.AccessToken))
                {
                    Console.WriteLine("✅ User signed in successfully.");
                    Console.WriteLine($"🔐 Supabase token: {session.AccessToken}");
                    return session.AccessToken;
                }

                Console.WriteLine("❌ Failed to sign in or token missing.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Exception during sign-in: {ex.Message}");
                return null;
            }
        }

        public async Task<User?> GetUserFromAccessToken(string accessToken)
        {
            await InitializeAsync();

            try
            {
                var user = await SupabaseClient.Auth.GetUser(accessToken);
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Error getting user: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> UploadImageToStorageAsync(IFormFile file)
        {
            await InitializeAsync();

            try
            {
                var tempPath = Path.GetTempFileName();

                await using (var fs = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(fs);
                }

                // Sanitize and create unique file name
                var sanitizedFileName = Path.GetFileNameWithoutExtension(file.FileName)
                    .Replace(" ", "_")
                    .Replace(",", "")
                    .Replace(":", "") + Path.GetExtension(file.FileName);

                // 👇 store under thumbnail/ folder
                var destinationPath = $"thumbnail/{Guid.NewGuid()}_{sanitizedFileName}";

                var bucket = SupabaseClient.Storage.From("blogimages");

                await bucket.Upload(tempPath, destinationPath);

                File.Delete(tempPath);

                var publicUrl = bucket.GetPublicUrl(destinationPath);
                Console.WriteLine($"✅ Uploaded to Supabase: {publicUrl}");

                return publicUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to upload image: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteImageFromStorageAsync(string imageUrl)
        {
            await InitializeAsync();

            try
            {
                var path = ExtractRelativePath(imageUrl);

                if (string.IsNullOrWhiteSpace(path))
                {
                    Console.WriteLine("⚠️ Could not extract valid path from URL.");
                    return false;
                }

                var bucket = SupabaseClient.Storage.From("blogimages");

                // Remove returns a list of FileObject (removed files)
                var result = await bucket.Remove(new List<string> { path });

                if (result != null && result.Count > 0)
                {
                    Console.WriteLine($"🗑️ Deleted image: {path}");
                    return true;
                }

                Console.WriteLine($"⚠️ No files deleted for path: {path}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to delete image: {ex.Message}");
                return false;
            }
        }

        private string ExtractRelativePath(string imageUrl)
        {
            // Supabase public path format: https://{project}.supabase.co/storage/v1/object/public/{bucket}/{relativePath}
            var marker = "/object/public/blogimages/";
            var index = imageUrl.IndexOf(marker);

            if (index >= 0)
                return imageUrl.Substring(index + marker.Length);

            return "";
        }

        public async Task<Supabase.Client> GetSupabaseClientAsync()
        {
            await InitializeAsync();
            return SupabaseClient;
        }

    }
}
