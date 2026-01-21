using CoreBank.Application.Common.Models;
using MediatR;

namespace CoreBank.Application.Kyc.Commands.SubmitKycDocument;

public record SubmitKycDocumentCommand : IRequest<Result<SubmitKycDocumentResponse>>
{
    public Guid UserId { get; init; }
    public string DocumentType { get; init; } = null!;
    public string DocumentNumber { get; init; } = null!;
    public string? DocumentUrl { get; init; }
    public DateTime? ExpiryDate { get; init; }
}

public record SubmitKycDocumentResponse
{
    public Guid DocumentId { get; init; }
    public string DocumentType { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string Message { get; init; } = null!;
}
