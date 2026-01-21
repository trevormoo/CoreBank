using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using CoreBank.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Kyc.Queries.GetPendingKycDocuments;

public class GetPendingKycDocumentsQueryHandler
    : IRequestHandler<GetPendingKycDocumentsQuery, Result<PaginatedList<PendingKycDocumentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetPendingKycDocumentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<PendingKycDocumentDto>>> Handle(
        GetPendingKycDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.KycDocuments
            .Include(k => k.User)
            .Where(k => k.Status == KycStatus.Pending && !k.IsDeleted)
            .OrderBy(k => k.CreatedAt)
            .Select(k => new PendingKycDocumentDto
            {
                Id = k.Id,
                UserId = k.UserId,
                UserFullName = k.User.FirstName + " " + k.User.LastName,
                UserEmail = k.User.Email,
                DocumentType = k.DocumentType,
                DocumentNumber = k.DocumentNumber,
                DocumentUrl = k.DocumentUrl,
                ExpiryDate = k.ExpiryDate,
                SubmittedAt = k.CreatedAt
            });

        var result = await PaginatedList<PendingKycDocumentDto>.CreateAsync(
            query,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return result;
    }
}
