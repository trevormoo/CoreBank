using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Accounts.Queries.GetAccountById;

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, Result<AccountDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAccountByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AccountDto>> Handle(
        GetAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && !a.IsDeleted, cancellationToken);

        if (account is null)
            return Result.Failure<AccountDto>("Account not found", "ACCOUNT_NOT_FOUND");

        // Authorization check - if requesting user is specified and doesn't own the account
        if (request.RequestingUserId.HasValue && account.UserId != request.RequestingUserId)
            return Result.Failure<AccountDto>("Access denied", "ACCESS_DENIED");

        return new AccountDto
        {
            Id = account.Id,
            AccountNumber = account.AccountNumber,
            UserId = account.UserId,
            UserFullName = account.User.FullName,
            AccountType = account.AccountType,
            Balance = account.Balance,
            Currency = account.Currency,
            Status = account.Status,
            DailyWithdrawalLimit = account.DailyWithdrawalLimit,
            DailyWithdrawalUsed = account.DailyWithdrawalUsed,
            InterestRate = account.InterestRate,
            CreatedAt = account.CreatedAt
        };
    }
}
