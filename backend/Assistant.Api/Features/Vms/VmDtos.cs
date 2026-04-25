using Assistant.Api.Models;
using Assistant.Api.Services;

namespace Assistant.Api.Features.Vms;

public sealed record VmRequest(
    string Name,
    string? Hostname,
    string? IpAddress,
    string? Description,
    List<VmAccountRequest> Accounts,
    List<VmUrlRequest> Urls);

public sealed record VmAccountRequest(string Label, string Username, string? Password, string? Notes);

public sealed record VmUrlRequest(string Label, string Url);

public sealed record VmResponse(
    int Id,
    string Name,
    string? Hostname,
    string? IpAddress,
    string? Description,
    List<VmAccountResponse> Accounts,
    List<VmUrlResponse> Urls,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record VmAccountResponse(int Id, string Label, string Username, string? Password, string? Notes);

public sealed record VmUrlResponse(int Id, string Label, string Url);

public static class VmMapping
{
    public static VmResponse ToVmResponse(this ManagedVm vm, IPasswordCipher cipher) => new(
        vm.Id,
        vm.Name,
        vm.Hostname,
        vm.IpAddress,
        vm.Description,
        vm.Accounts
            .Select(account => new VmAccountResponse(
                account.Id,
                account.Label,
                account.Username,
                cipher.Decrypt(account.EncryptedPassword),
                account.Notes))
            .ToList(),
        vm.Urls
            .Select(url => new VmUrlResponse(url.Id, url.Label, url.Url))
            .ToList(),
        vm.CreatedAt,
        vm.UpdatedAt);
}
