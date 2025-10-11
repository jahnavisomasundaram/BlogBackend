using BlogAppBackend.Services;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://localhost:7157", "http://localhost:5089")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

//builder.Services.AddSignalR();

//builder.Services.AddSignalR().AddHubOptions<NotificationHub>(options =>
//{
//    options.EnableDetailedErrors = true;
//});


builder.Services.AddSingleton<RegisterServices>();

builder.Services.AddSingleton<SupabaseService>();

builder.Services.AddHttpClient<ApiAuth>();

//builder.Services.AddSingleton<IUserIdProvider, EmailBasedUserIdProvider>();


var app = builder.Build();

//app.MapHub<NotificationHub>("/notificationhub");


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
