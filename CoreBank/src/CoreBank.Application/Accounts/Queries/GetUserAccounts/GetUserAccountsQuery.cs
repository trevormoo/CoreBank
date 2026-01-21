using CoreBank.Application.Accounts.Queries.GetAccountById;
using CoreBank.Application.Common.Models;
using MediatR;

namespace CoreBank.Application.Accounts.Queries.GetUserAccounts;

public record GetUserAccountsQuery : IRequest<Result<List<AccountDto>>>
{
    public Guid UserId { get; init; }
}
