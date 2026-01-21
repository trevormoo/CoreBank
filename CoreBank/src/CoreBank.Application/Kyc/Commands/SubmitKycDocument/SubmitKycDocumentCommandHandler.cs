using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Kyc.Commands.SubmitKycDocument;

public class SubmitKycDocumentCommandHandler
    : IRequestHandler<SubmitKycDocumentCommand, Result<SubmitKycDocumentResponse>>
{
    private readonly IApplicationDbContext _context;

    public SubmitKycDocumentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SubmitKycDocumentResponse>> Handle(
        SubmitKycDocumentCommand request,
        CancellationToken cancellationToken)
    {
        // Verify user exists
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

        if (user is null)
            return Result.Failure<SubmitKycDocumentResponse>("User not found", "USER_NOT_FOUND");

        // Check for duplicate document number
        var existingDocument = await _context.KycDocuments
            .FirstOrDefaultAsync(k =>
                k.DocumentNumber == request.DocumentNumber &&
                k.DocumentType == request.DocumentType &&
                !k.IsDeleted,
                cancellationToken);

        if (existingDocument is not null)
            return Result.Failure<SubmitKycDocumentResponse>(
                "A document with this number already exists",
                "DUPLICATE_DOCUMENT");

        // Check if user already has a pending document of this type
        var pendingDocument = await _context.KycDocuments
            .FirstOrDefaultAsync(k =>
                k.UserId == request.UserId &&
                k.DocumentType == request.DocumentType &&
                k.Status == KycStatus.Pending &&
                !k.IsDeleted,
                cancellationToken);

        if (pendingDocument is not null)
            return Result.Failure<SubmitKycDocumentResponse>(
                "You already have a pending document of this type",
                "PENDING_DOCUMENT_EXISTS");

        // Create KYC document
        var document = KycDocument.Create(
            request.UserId,
            request.DocumentType,
            request.DocumentNumber,
            request.DocumentUrl,
            request.ExpiryDate);

        _context.KycDocuments.Add(document);

        // Update user KYC status to pending if not already approved
        if (user.KycStatus != KycStatus.Approved)
        {
            user.UpdateKycStatus(KycStatus.Pending);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SubmitKycDocumentResponse
        {
            DocumentId = document.Id,
            DocumentType = document.DocumentType,
            Status = document.Status.ToString(),
            Message = "KYC document submitted successfully. It will be reviewed shortly."
        };
    }
}
