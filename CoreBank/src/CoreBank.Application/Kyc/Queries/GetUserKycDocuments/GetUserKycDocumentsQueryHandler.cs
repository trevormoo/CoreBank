using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Kyc.Queries.GetUserKycDocuments;

public class GetUserKycDocumentsQueryHandler
    : IRequestHandler<GetUserKycDocumentsQuery, Result<List<KycDocumentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetUserKycDocumentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<KycDocumentDto>>> Handle(
        GetUserKycDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        // Authorization check
        if (request.RequestingUserId.HasValue && request.RequestingUserId != request.UserId)
            return Result.Failure<List<KycDocumentDto>>("Access denied", "ACCESS_DENIED");

        var documents = await _context.KycDocuments
            .Where(k => k.UserId == request.UserId && !k.IsDeleted)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new KycDocumentDto
            {
                Id = k.Id,
                DocumentType = k.DocumentType,
                DocumentNumber = k.DocumentNumber,
                DocumentUrl = k.DocumentUrl,
                ExpiryDate = k.ExpiryDate,
                Status = k.Status,
                RejectionReason = k.RejectionReason,
                ReviewedAt = k.ReviewedAt,
                CreatedAt = k.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return documents;
    }
}
