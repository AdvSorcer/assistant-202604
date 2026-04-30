using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Assistant.Api.Services;

public sealed class DatabaseBackupOptions
{
    public bool Enabled { get; set; } = true;
    public string TimeOfDay { get; set; } = "02:00";
    public string TimeZone { get; set; } = "Asia/Taipei";
    public int RetentionCount { get; set; } = 30;
}

public sealed class DatabaseBackupService(
    IConfiguration configuration,
    IOptions<DatabaseBackupOptions> options,
    ILogger<DatabaseBackupService> logger)
{
    private readonly SemaphoreSlim _backupLock = new(1, 1);
    private TimeZoneInfo BackupTimeZone => ResolveTimeZone(options.Value.TimeZone);

    public async Task BackupIfStaleAsync(CancellationToken cancellationToken)
    {
        var backupDirectory = GetBackupDirectory();
        var latestBackup = Directory.Exists(backupDirectory)
            ? Directory.EnumerateFiles(backupDirectory, "*.db").Select(File.GetCreationTimeUtc).DefaultIfEmpty(DateTime.MinValue).Max()
            : DateTime.MinValue;

        if (DateTime.UtcNow - latestBackup >= TimeSpan.FromHours(24))
        {
            await BackupAsync(cancellationToken);
        }
    }

    public async Task BackupAsync(CancellationToken cancellationToken)
    {
        if (!await _backupLock.WaitAsync(0, cancellationToken))
        {
            logger.LogInformation("Database backup skipped because another backup is already running.");
            return;
        }

        try
        {
            var backupDirectory = GetBackupDirectory();
            Directory.CreateDirectory(backupDirectory);

            var timestamp = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, BackupTimeZone).ToString("yyyyMMdd-HHmmss");
            var backupPath = Path.Combine(backupDirectory, $"assistant-{timestamp}.db");
            var tempPath = $"{backupPath}.tmp";

            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            try
            {
                var connectionString = GetConnectionString();
                await using var source = new SqliteConnection(connectionString);
                await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = tempPath
                }.ToString());

                await source.OpenAsync(cancellationToken);
                await destination.OpenAsync(cancellationToken);
                source.BackupDatabase(destination);

                File.Move(tempPath, backupPath);
            }
            catch
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                throw;
            }

            logger.LogInformation("Database backup created: {BackupPath}", backupPath);

            CleanupOldBackups(backupDirectory);
        }
        finally
        {
            _backupLock.Release();
        }
    }

    public DateTimeOffset GetNextRun(DateTimeOffset now)
    {
        var backupTime = ParseTimeOfDay(options.Value.TimeOfDay);
        var nowInBackupTimeZone = TimeZoneInfo.ConvertTime(now, BackupTimeZone);
        var nextRun = CreateZonedDateTime(nowInBackupTimeZone.Date.Add(backupTime), BackupTimeZone);
        if (nextRun <= nowInBackupTimeZone)
        {
            nextRun = CreateZonedDateTime(nowInBackupTimeZone.Date.AddDays(1).Add(backupTime), BackupTimeZone);
        }

        return nextRun;
    }

    private string GetBackupDirectory()
    {
        var dataSource = new SqliteConnectionStringBuilder(GetConnectionString()).DataSource;
        var dataDirectory = Path.GetDirectoryName(Path.GetFullPath(dataSource)) ?? AppContext.BaseDirectory;
        return Path.Combine(dataDirectory, "backups", "db");
    }

    private string GetConnectionString() =>
        configuration.GetConnectionString("Default") ?? "Data Source=./data/assistant.db";

    private void CleanupOldBackups(string backupDirectory)
    {
        var retentionCount = Math.Max(1, options.Value.RetentionCount);
        var oldBackups = Directory
            .EnumerateFiles(backupDirectory, "*.db")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .Skip(retentionCount)
            .ToList();

        foreach (var backup in oldBackups)
        {
            backup.Delete();
            logger.LogInformation("Old database backup deleted: {BackupPath}", backup.FullName);
        }
    }

    private static TimeSpan ParseTimeOfDay(string value)
    {
        return TimeSpan.TryParse(value, out var parsed) && parsed >= TimeSpan.Zero && parsed < TimeSpan.FromDays(1)
            ? parsed
            : TimeSpan.FromHours(2);
    }

    private static DateTimeOffset CreateZonedDateTime(DateTime localDateTime, TimeZoneInfo timeZone)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, timeZone.GetUtcOffset(unspecified));
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(timeZoneId) ? "Asia/Taipei" : timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
        }
    }
}

public sealed class DatabaseBackupWorker(
    DatabaseBackupService backupService,
    IOptions<DatabaseBackupOptions> options,
    ILogger<DatabaseBackupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Automatic database backup is disabled.");
            return;
        }

        await RunStartupBackupIfNeeded(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRun = backupService.GetNextRun(DateTimeOffset.Now);
                var delay = nextRun - DateTimeOffset.Now;
                if (delay > TimeSpan.Zero)
                {
                    logger.LogInformation("Next automatic database backup scheduled at {NextRun}.", nextRun);
                    await Task.Delay(delay, stoppingToken);
                }

                await backupService.BackupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Automatic database backup failed.");
                await DelayAfterFailure(stoppingToken);
            }
        }
    }

    private async Task RunStartupBackupIfNeeded(CancellationToken stoppingToken)
    {
        try
        {
            await backupService.BackupIfStaleAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Startup database backup check failed.");
        }
    }

    private static async Task DelayAfterFailure(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
