using CoreBank.Application.Common.Models;
using MediatR;

namespace CoreBank.Application.AuditLogs.Queries.GetAuditLogs;

public record GetAuditLogsQuery : IRequest<Result<PaginatedList<AuditLogDto>>>
{
    public string? UserId { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public string? Action { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public record AuditLogDto
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = null!;
    public string Action { get; init; } = null!;
    public string EntityType { get; init; } = null!;
    public string EntityId { get; init; } = null!;
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime Timestamp { get; init; }
}
