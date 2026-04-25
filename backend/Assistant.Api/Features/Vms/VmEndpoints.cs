using Assistant.Api.Data;
using Assistant.Api.Models;
using Assistant.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Api.Features.Vms;

public static class VmEndpoints
{
    public static RouteGroupBuilder MapVmEndpoints(this RouteGroupBuilder api)
    {
        var vmApi = api.MapGroup("/vms");

        vmApi.MapGet("/", async (AssistantDbContext db, IPasswordCipher cipher) =>
        {
            var vms = await db.Vms
                .AsNoTracking()
                .Include(vm => vm.Accounts)
                .Include(vm => vm.Urls)
                .OrderBy(vm => vm.Name)
                .ToListAsync();

            return vms.Select(vm => vm.ToVmResponse(cipher));
        });

        vmApi.MapGet("/{id:int}", async (int id, AssistantDbContext db, IPasswordCipher cipher) =>
        {
            var vm = await db.Vms
                .AsNoTracking()
                .Include(item => item.Accounts)
                .Include(item => item.Urls)
                .FirstOrDefaultAsync(item => item.Id == id);

            return vm is null ? Results.NotFound() : Results.Ok(vm.ToVmResponse(cipher));
        });

        vmApi.MapPost("/", async (VmRequest request, AssistantDbContext db, IPasswordCipher cipher) =>
        {
            var validationResult = ValidateVmRequest(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var vm = new ManagedVm
            {
                Name = request.Name.Trim(),
                Hostname = request.Hostname,
                IpAddress = request.IpAddress,
                Description = request.Description,
                Accounts = request.Accounts.Select(account => new VmAccount
                {
                    Label = account.Label.Trim(),
                    Username = account.Username.Trim(),
                    EncryptedPassword = cipher.Encrypt(account.Password),
                    Notes = account.Notes
                }).ToList(),
                Urls = request.Urls.Select(url => new VmUrl
                {
                    Label = url.Label.Trim(),
                    Url = url.Url.Trim()
                }).ToList()
            };

            db.Vms.Add(vm);
            await db.SaveChangesAsync();
            return Results.Created($"/api/vms/{vm.Id}", vm.ToVmResponse(cipher));
        });

        vmApi.MapPut("/{id:int}", async (int id, VmRequest request, AssistantDbContext db, IPasswordCipher cipher) =>
        {
            var validationResult = ValidateVmRequest(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var vm = await db.Vms
                .Include(item => item.Accounts)
                .Include(item => item.Urls)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (vm is null)
            {
                return Results.NotFound();
            }

            vm.Name = request.Name.Trim();
            vm.Hostname = request.Hostname;
            vm.IpAddress = request.IpAddress;
            vm.Description = request.Description;
            vm.UpdatedAt = DateTimeOffset.UtcNow;

            db.VmAccounts.RemoveRange(vm.Accounts);
            db.VmUrls.RemoveRange(vm.Urls);
            vm.Accounts = request.Accounts.Select(account => new VmAccount
            {
                Label = account.Label.Trim(),
                Username = account.Username.Trim(),
                EncryptedPassword = cipher.Encrypt(account.Password),
                Notes = account.Notes
            }).ToList();
            vm.Urls = request.Urls.Select(url => new VmUrl
            {
                Label = url.Label.Trim(),
                Url = url.Url.Trim()
            }).ToList();

            await db.SaveChangesAsync();
            return Results.Ok(vm.ToVmResponse(cipher));
        });

        vmApi.MapDelete("/{id:int}", async (int id, AssistantDbContext db) =>
        {
            var deleted = await db.Vms.Where(vm => vm.Id == id).ExecuteDeleteAsync();
            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });

        return api;
    }

    private static IResult? ValidateVmRequest(VmRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { message = "請輸入 VM 名稱" });
        }

        if (request.Urls.Any(url => !string.IsNullOrWhiteSpace(url.Url) && !Uri.TryCreate(url.Url, UriKind.Absolute, out _)))
        {
            return Results.BadRequest(new { message = "網址格式不正確" });
        }

        return null;
    }
}
