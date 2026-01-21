using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Transactions.Queries.GetTransactionHistory;

public class GetTransactionHistoryQueryHandler
    : IRequestHandler<GetTransactionHistoryQuery, Result<PaginatedList<TransactionDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetTransactionHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<TransactionDto>>> Handle(
        GetTransactionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        // Verify account exists and user has access
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account is null)
            return Result.Failure<PaginatedList<TransactionDto>>("Account not found", "ACCOUNT_NOT_FOUND");

        // Check authorization if RequestingUserId is provided
        if (request.RequestingUserId.HasValue && account.UserId != request.RequestingUserId.Value)
            return Result.Failure<PaginatedList<TransactionDto>>("Access denied", "ACCESS_DENIED");

        // Build query for transactions where this account is source or destination
        var query = _context.Transactions
            .Include(t => t.SourceAccount)
            .Include(t => t.DestinationAccount)
            .Where(t => t.SourceAccountId == request.AccountId || t.DestinationAccountId == request.AccountId)
            .AsQueryable();

        // Apply filters
        if (request.Type.HasValue)
            query = query.Where(t => t.Type == request.Type.Value);

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        if (request.FromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(t => t.CreatedAt <= request.ToDate.Value);

        // Order by most recent first
        query = query.OrderByDescending(t => t.CreatedAt);

        // Project to DTO
        var dtoQuery = query.Select(t => new TransactionDto
        {
            Id = t.Id,
            ReferenceNumber = t.ReferenceNumber,
            Type = t.Type,
            Amount = t.Amount,
            Currency = t.Currency,
            Status = t.Status,
            Description = t.Description,
            SourceAccountId = t.SourceAccountId,
            SourceAccountNumber = t.SourceAccount != null ? t.SourceAccount.AccountNumber : null,
            DestinationAccountId = t.DestinationAccountId,
            DestinationAccountNumber = t.DestinationAccount != null ? t.DestinationAccount.AccountNumber : null,
            BalanceAfter = t.SourceAccountId == request.AccountId
                ? t.BalanceAfterSource
                : t.BalanceAfterDestination,
            FailureReason = t.FailureReason,
            CreatedAt = t.CreatedAt
        });

        var paginatedList = await PaginatedList<TransactionDto>.CreateAsync(
            dtoQuery,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return paginatedList;
    }
}
