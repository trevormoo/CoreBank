using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using CoreBank.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Kyc.Commands.ReviewKycDocument;

public class ReviewKycDocumentCommandHandler
    : IRequestHandler<ReviewKycDocumentCommand, Result<ReviewKycDocumentResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public ReviewKycDocumentCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<Result<ReviewKycDocumentResponse>> Handle(
        ReviewKycDocumentCommand request,
        CancellationToken cancellationToken)
    {
        // Find the document
        var document = await _context.KycDocuments
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == request.DocumentId && !k.IsDeleted, cancellationToken);

        if (document is null)
            return Result.Failure<ReviewKycDocumentResponse>("Document not found", "DOCUMENT_NOT_FOUND");

        if (document.Status != KycStatus.Pending)
            return Result.Failure<ReviewKycDocumentResponse>(
                "Document has already been reviewed",
                "ALREADY_REVIEWED");

        var user = document.User;

        if (request.Approve)
        {
            document.Approve(request.ReviewerId);
            user.UpdateKycStatus(KycStatus.Approved);

            await _emailService.SendAccountNotificationAsync(
                user.Email,
                "KYC Approved",
                "Your KYC document has been approved. You now have full access to all banking features.",
                cancellationToken);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.RejectionReason))
                return Result.Failure<ReviewKycDocumentResponse>(
                    "Rejection reason is required",
                    "REJECTION_REASON_REQUIRED");

            document.Reject(request.ReviewerId, request.RejectionReason);
            user.UpdateKycStatus(KycStatus.Rejected);

            await _emailService.SendAccountNotificationAsync(
                user.Email,
                "KYC Rejected",
                $"Your KYC document has been rejected. Reason: {request.RejectionReason}. Please submit a new document.",
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ReviewKycDocumentResponse
        {
            DocumentId = document.Id,
            Status = document.Status.ToString(),
            UserKycStatus = user.KycStatus.ToString(),
            Message = request.Approve
                ? "KYC document approved successfully"
                : "KYC document rejected"
        };
    }
}
