using Assistant.Api.Features.Logs;
using Assistant.Api.Features.Todos;
using Assistant.Api.Features.Vms;
using Assistant.Api.Features.Wiki;

namespace Assistant.Api.Features.Backup;

public sealed record BackupResponse(
    DateTimeOffset ExportedAt,
    List<VmResponse> Vms,
    List<DailyLogResponse> Logs,
    List<TodoResponse> Todos,
    List<WikiPageResponse> WikiPages);

public sealed record BackupImportRequest(
    DateTimeOffset? ExportedAt,
    List<VmResponse>? Vms,
    List<DailyLogResponse>? Logs,
    List<TodoResponse>? Todos,
    List<WikiPageResponse>? WikiPages);

public sealed record BackupImportPreviewResponse(
    int Vms,
    int Logs,
    int Todos,
    int WikiPages,
    List<string> Warnings);
