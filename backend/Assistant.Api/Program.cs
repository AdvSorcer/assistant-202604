using Assistant.Api.Infrastructure;
using Assistant.Api.Features.Auth;
using Assistant.Api.Features.AiWeeklyReports;
using Assistant.Api.Features.Backup;
using Assistant.Api.Features.Health;
using Assistant.Api.Features.Logs;
using Assistant.Api.Features.Settings;
using Assistant.Api.Features.Todos;
using Assistant.Api.Features.Vms;
using Assistant.Api.Features.Wiki;
using Assistant.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Data;
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
builder.Services.AddScoped<IPasswordCipher, PasswordCipher>();
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddHttpClient();
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
    EnsureCompatibleSchema(db);
    SettingsEndpoints.EnsureSecurityDefaults(db, builder.Configuration);
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

api.MapHealthEndpoints();
api.MapAuthEndpoints();
api.MapVmEndpoints();
api.MapLogEndpoints();
api.MapTodoEndpoints();
api.MapWikiEndpoints();
api.MapBackupEndpoints();
api.MapSettingsEndpoints();
api.MapAiWeeklyReportEndpoints();

app.Run();

static void EnsureCompatibleSchema(AssistantDbContext db)
{
    EnsureBooleanColumn(db, "Vms", "IsFavorite");
    EnsureBooleanColumn(db, "WikiPages", "IsPinned");
    EnsureAppSettingsTable(db);
}

static void EnsureBooleanColumn(AssistantDbContext db, string tableName, string columnName)
{
    var connection = db.Database.GetDbConnection();
    if (connection.State != ConnectionState.Open)
    {
        connection.Open();
    }

    using var checkCommand = connection.CreateCommand();
    checkCommand.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name = '{columnName}'";
    var exists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
    if (exists)
    {
        return;
    }

    using var alterCommand = connection.CreateCommand();
    alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} INTEGER NOT NULL DEFAULT 0";
    alterCommand.ExecuteNonQuery();
}

static void EnsureAppSettingsTable(AssistantDbContext db)
{
    var connection = db.Database.GetDbConnection();
    if (connection.State != ConnectionState.Open)
    {
        connection.Open();
    }

    using var command = connection.CreateCommand();
    command.CommandText = """
        CREATE TABLE IF NOT EXISTS AppSettings (
            Id INTEGER NOT NULL CONSTRAINT PK_AppSettings PRIMARY KEY AUTOINCREMENT,
            Key TEXT NOT NULL,
            Value TEXT NULL,
            UpdatedAt TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS IX_AppSettings_Key ON AppSettings (Key);
        """;
    command.ExecuteNonQuery();
}
