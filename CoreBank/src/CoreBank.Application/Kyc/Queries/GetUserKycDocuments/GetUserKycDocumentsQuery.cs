using CoreBank.Application.Common.Models;
using CoreBank.Domain.Enums;
using MediatR;

namespace CoreBank.Application.Kyc.Queries.GetUserKycDocuments;

public record GetUserKycDocumentsQuery : IRequest<Result<List<KycDocumentDto>>>
{
    public Guid UserId { get; init; }
    public Guid? RequestingUserId { get; init; }
}

public record KycDocumentDto
{
    public Guid Id { get; init; }
    public string DocumentType { get; init; } = null!;
    public string DocumentNumber { get; init; } = null!;
    public string? DocumentUrl { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public KycStatus Status { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
