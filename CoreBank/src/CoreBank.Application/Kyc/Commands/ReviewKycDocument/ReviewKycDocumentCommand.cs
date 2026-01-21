using CoreBank.Application.Common.Models;
using MediatR;

namespace CoreBank.Application.Kyc.Commands.ReviewKycDocument;

public record ReviewKycDocumentCommand : IRequest<Result<ReviewKycDocumentResponse>>
{
    public Guid DocumentId { get; init; }
    public string ReviewerId { get; init; } = null!;
    public bool Approve { get; init; }
    public string? RejectionReason { get; init; }
}

public record ReviewKycDocumentResponse
{
    public Guid DocumentId { get; init; }
    public string Status { get; init; } = null!;
    public string UserKycStatus { get; init; } = null!;
    public string Message { get; init; } = null!;
}
