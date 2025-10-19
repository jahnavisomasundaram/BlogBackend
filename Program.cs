using BlogAppBackend.Services;
// You might need other 'using' statements depending on your project
// using Microsoft.AspNetCore.SignalR; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- IMPROVED CORS POLICY ---
// Reads the frontend URL from environment variables for production,
// but defaults to localhost if not found (for local development).
var frontendUrl = builder.Configuration["FRONTEND_URL"] ?? "http://localhost:5089";

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(frontendUrl, "https://blogfrontend-whao.onrender.com", "http://localhost:5089")
              .AllowAnyHeader()
              .AllowAnyMethod());
});


// Register your custom services
builder.Services.AddSingleton<RegisterServices>();
builder.Services.AddSingleton<SupabaseService>();
builder.Services.AddHttpClient<ApiAuth>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- APPLY THE CORS POLICY ---
// This single line applies the default policy defined above.
app.UseCors();

app.UseAuthorization();

app.MapControllers();

// --- ROOT ENDPOINT TO FIX 404 ERROR ---
// This handles requests to the base URL (e.g., your .onrender.com address)
app.MapGet("/", () => Results.Ok("BlogBackend API is running!"));

app.Run();