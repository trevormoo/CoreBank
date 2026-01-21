using System.Text.Json;
using CoreBank.Application.Common.Interfaces;
using CoreBank.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace CoreBank.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        IApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string userId,
        string action,
        string entityType,
        string entityId,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

        var auditLog = AuditLog.Create(
            userId,
            action,
            entityType,
            entityId,
            oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            newValues != null ? JsonSerializer.Serialize(newValues) : null,
            ipAddress,
            userAgent);

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
