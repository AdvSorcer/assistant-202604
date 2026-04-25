using Assistant.Api.Data;
using Assistant.Api.Features.Logs;
using Assistant.Api.Features.Todos;
using Assistant.Api.Features.Vms;
using Assistant.Api.Features.Wiki;
using Assistant.Api.Models;
using Assistant.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Api.Features.Backup;

public static class BackupEndpoints
{
    public static RouteGroupBuilder MapBackupEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/backup/export", async (AssistantDbContext db, IPasswordCipher cipher) =>
        {
            var vms = await db.Vms
                .AsNoTracking()
                .Include(vm => vm.Accounts)
                .Include(vm => vm.Urls)
                .OrderByDescending(vm => vm.IsFavorite)
                .ThenBy(vm => vm.Name)
                .ToListAsync();
            var logs = await db.DailyLogs.AsNoTracking().OrderByDescending(log => log.Date).ToListAsync();
            var todos = await db.TodoItems.AsNoTracking().OrderBy(todo => todo.Status).ThenBy(todo => todo.DueDate).ToListAsync();
            var wikiPages = await db.WikiPages.AsNoTracking().OrderByDescending(page => page.IsPinned).ThenBy(page => page.Title).ToListAsync();

            return Results.Ok(new BackupResponse(
                DateTimeOffset.UtcNow,
                vms.Select(vm => vm.ToVmResponse(cipher)).ToList(),
                logs.Select(log => log.ToDailyLogResponse()).ToList(),
                todos.Select(todo => todo.ToTodoResponse()).ToList(),
                wikiPages.Select(page => page.ToWikiPageResponse()).ToList()));
        });

        api.MapPost("/backup/preview-import", (BackupImportRequest request) =>
            Results.Ok(CreatePreview(request)));

        api.MapPost("/backup/import", async (BackupImportRequest request, AssistantDbContext db, IPasswordCipher cipher) =>
        {
            var preview = CreatePreview(request);
            if (preview.Warnings.Count > 0)
            {
                return Results.BadRequest(new { message = "備份檔驗證失敗", warnings = preview.Warnings });
            }

            await using var transaction = await db.Database.BeginTransactionAsync();

            await db.VmAccounts.ExecuteDeleteAsync();
            await db.VmUrls.ExecuteDeleteAsync();
            await db.Vms.ExecuteDeleteAsync();
            await db.DailyLogs.ExecuteDeleteAsync();
            await db.TodoItems.ExecuteDeleteAsync();
            await db.WikiPages.ExecuteDeleteAsync();

            db.Vms.AddRange(request.Vms!.Select(vm => new ManagedVm
            {
                Name = vm.Name.Trim(),
                Hostname = vm.Hostname,
                IpAddress = vm.IpAddress,
                Description = vm.Description,
                IsFavorite = vm.IsFavorite,
                CreatedAt = vm.CreatedAt,
                UpdatedAt = vm.UpdatedAt,
                Accounts = vm.Accounts.Select(account => new VmAccount
                {
                    Label = account.Label.Trim(),
                    Username = account.Username.Trim(),
                    EncryptedPassword = cipher.Encrypt(account.Password),
                    Notes = account.Notes
                }).ToList(),
                Urls = vm.Urls.Select(url => new VmUrl
                {
                    Label = url.Label.Trim(),
                    Url = url.Url.Trim()
                }).ToList()
            }));

            db.DailyLogs.AddRange(request.Logs!.Select(log => new DailyLog
            {
                Date = log.Date,
                Content = log.Content,
                CreatedAt = log.CreatedAt,
                UpdatedAt = log.UpdatedAt
            }));

            db.TodoItems.AddRange(request.Todos!.Select(todo => new TodoItem
            {
                Title = todo.Title.Trim(),
                Description = todo.Description,
                DueDate = todo.DueDate,
                Status = todo.Status,
                CompletedAt = todo.CompletedAt,
                CreatedAt = todo.CreatedAt,
                UpdatedAt = todo.UpdatedAt
            }));

            db.WikiPages.AddRange(request.WikiPages!.Select(page => new WikiPage
            {
                Title = page.Title.Trim(),
                Slug = page.Slug.Trim(),
                Content = page.Content,
                IsPinned = page.IsPinned,
                CreatedAt = page.CreatedAt,
                UpdatedAt = page.UpdatedAt
            }));

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Results.Ok(preview);
        });

        return api;
    }

    private static BackupImportPreviewResponse CreatePreview(BackupImportRequest request)
    {
        var warnings = new List<string>();
        var vms = request.Vms ?? [];
        var logs = request.Logs ?? [];
        var todos = request.Todos ?? [];
        var wikiPages = request.WikiPages ?? [];

        if (request.Vms is null || request.Logs is null || request.Todos is null || request.WikiPages is null)
        {
            warnings.Add("備份檔缺少必要欄位");
        }

        warnings.AddRange(vms.Where(vm => string.IsNullOrWhiteSpace(vm.Name)).Select(_ => "VM 名稱不可為空"));
        warnings.AddRange(vms
            .SelectMany(vm => vm.Urls)
            .Where(url => !string.IsNullOrWhiteSpace(url.Url) && !Uri.TryCreate(url.Url, UriKind.Absolute, out _))
            .Select(url => $"VM 網址格式不正確：{url.Url}"));
        warnings.AddRange(logs.Where(log => string.IsNullOrWhiteSpace(log.Content)).Select(log => $"{log.Date} 的日誌內容不可為空"));
        warnings.AddRange(todos.Where(todo => string.IsNullOrWhiteSpace(todo.Title)).Select(_ => "代辦標題不可為空"));
        warnings.AddRange(wikiPages.Where(page => string.IsNullOrWhiteSpace(page.Title) || string.IsNullOrWhiteSpace(page.Slug)).Select(_ => "Wiki 標題與 Slug 不可為空"));

        var duplicateLogDates = logs
            .GroupBy(log => log.Date)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key.ToString());
        warnings.AddRange(duplicateLogDates.Select(date => $"備份檔內有重複日誌日期：{date}"));

        var duplicateWikiSlugs = wikiPages
            .GroupBy(page => page.Slug.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            .Select(group => group.Key);
        warnings.AddRange(duplicateWikiSlugs.Select(slug => $"備份檔內有重複 Wiki Slug：{slug}"));

        return new BackupImportPreviewResponse(vms.Count, logs.Count, todos.Count, wikiPages.Count, warnings.Distinct().ToList());
    }
}
