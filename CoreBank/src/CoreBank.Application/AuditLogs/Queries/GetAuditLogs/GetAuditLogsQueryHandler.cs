using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.AuditLogs.Queries.GetAuditLogs;

public class GetAuditLogsQueryHandler
    : IRequestHandler<GetAuditLogsQuery, Result<PaginatedList<AuditLogDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAuditLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<AuditLogDto>>> Handle(
        GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.UserId))
            query = query.Where(a => a.UserId == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);

        if (!string.IsNullOrWhiteSpace(request.EntityId))
            query = query.Where(a => a.EntityId == request.EntityId);

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action.Contains(request.Action));

        if (request.FromDate.HasValue)
            query = query.Where(a => a.Timestamp >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.Timestamp <= request.ToDate.Value);

        // Order by most recent first
        query = query.OrderByDescending(a => a.Timestamp);

        // Project to DTO
        var dtoQuery = query.Select(a => new AuditLogDto
        {
            Id = a.Id,
            UserId = a.UserId,
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            OldValues = a.OldValues,
            NewValues = a.NewValues,
            IpAddress = a.IpAddress,
            UserAgent = a.UserAgent,
            Timestamp = a.Timestamp
        });

        var result = await PaginatedList<AuditLogDto>.CreateAsync(
            dtoQuery,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return result;
    }
}
