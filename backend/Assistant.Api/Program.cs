using Assistant.Api.Data;
using Assistant.Api.Features.Auth;
using Assistant.Api.Features.Backup;
using Assistant.Api.Features.Logs;
using Assistant.Api.Features.Todos;
using Assistant.Api.Features.Vms;
using Assistant.Api.Features.Wiki;
using Assistant.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://127.0.0.1:5173"];
var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=./data/assistant.db";
var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
var dataDirectory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
if (!string.IsNullOrWhiteSpace(dataDirectory))
{
    Directory.CreateDirectory(dataDirectory);
}

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddDbContext<AssistantDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddSingleton<IPasswordCipher, PasswordCipher>();
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(AuthEndpoints.AuthLoginRateLimitPolicy, limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AssistantDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseApiSessionAuth();

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new { status = "ok" }));

api.MapAuthEndpoints();
api.MapVmEndpoints();
api.MapLogEndpoints();
api.MapTodoEndpoints();
api.MapWikiEndpoints();
api.MapBackupEndpoints();

app.Run();
