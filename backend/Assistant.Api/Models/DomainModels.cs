namespace Assistant.Api.Models;

public sealed class ManagedVm
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Hostname { get; set; }
    public string? IpAddress { get; set; }
    public string? Description { get; set; }
    public bool IsFavorite { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<VmAccount> Accounts { get; set; } = [];
    public List<VmUrl> Urls { get; set; } = [];
}

public sealed class VmAccount
{
    public int Id { get; set; }
    public int ManagedVmId { get; set; }
    public required string Label { get; set; }
    public required string Username { get; set; }
    public string? EncryptedPassword { get; set; }
    public string? Notes { get; set; }
}

public sealed class VmUrl
{
    public int Id { get; set; }
    public int ManagedVmId { get; set; }
    public required string Label { get; set; }
    public required string Url { get; set; }
}

public sealed class DailyLog
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class TodoItem
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateOnly? DueDate { get; set; }
    public TodoStatus Status { get; set; } = TodoStatus.Todo;
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum TodoStatus
{
    Todo,
    Doing,
    Done,
    Archived
}

public sealed class WikiPage
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public required string Content { get; set; }
    public bool IsPinned { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AppSetting
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public string? Value { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
