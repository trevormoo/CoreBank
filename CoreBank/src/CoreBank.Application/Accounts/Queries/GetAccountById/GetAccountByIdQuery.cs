using CoreBank.Application.Common.Models;
using CoreBank.Domain.Enums;
using MediatR;

namespace CoreBank.Application.Accounts.Queries.GetAccountById;

public record GetAccountByIdQuery : IRequest<Result<AccountDto>>
{
    public Guid AccountId { get; init; }
    public Guid? RequestingUserId { get; init; } // For authorization
}

public record AccountDto
{
    public Guid Id { get; init; }
    public string AccountNumber { get; init; } = null!;
    public Guid UserId { get; init; }
    public string UserFullName { get; init; } = null!;
    public AccountType AccountType { get; init; }
    public decimal Balance { get; init; }
    public string Currency { get; init; } = null!;
    public AccountStatus Status { get; init; }
    public decimal DailyWithdrawalLimit { get; init; }
    public decimal DailyWithdrawalUsed { get; init; }
    public decimal InterestRate { get; init; }
    public DateTime CreatedAt { get; init; }
}
