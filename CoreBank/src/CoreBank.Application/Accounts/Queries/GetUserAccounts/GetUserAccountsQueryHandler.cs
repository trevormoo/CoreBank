using CoreBank.Application.Accounts.Queries.GetAccountById;
using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Accounts.Queries.GetUserAccounts;

public class GetUserAccountsQueryHandler : IRequestHandler<GetUserAccountsQuery, Result<List<AccountDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetUserAccountsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<AccountDto>>> Handle(
        GetUserAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var accounts = await _context.Accounts
            .Include(a => a.User)
            .Where(a => a.UserId == request.UserId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AccountDto
            {
                Id = a.Id,
                AccountNumber = a.AccountNumber,
                UserId = a.UserId,
                UserFullName = a.User.FullName,
                AccountType = a.AccountType,
                Balance = a.Balance,
                Currency = a.Currency,
                Status = a.Status,
                DailyWithdrawalLimit = a.DailyWithdrawalLimit,
                DailyWithdrawalUsed = a.DailyWithdrawalUsed,
                InterestRate = a.InterestRate,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return accounts;
    }
}
