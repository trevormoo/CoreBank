using CoreBank.Application.Common.Models;
using CoreBank.Domain.Enums;
using MediatR;

namespace CoreBank.Application.Kyc.Queries.GetPendingKycDocuments;

public record GetPendingKycDocumentsQuery : IRequest<Result<PaginatedList<PendingKycDocumentDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record PendingKycDocumentDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string UserFullName { get; init; } = null!;
    public string UserEmail { get; init; } = null!;
    public string DocumentType { get; init; } = null!;
    public string DocumentNumber { get; init; } = null!;
    public string? DocumentUrl { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public DateTime SubmittedAt { get; init; }
}
