using CoreBank.Application.Common.Models;
using CoreBank.Domain.Enums;
using MediatR;

namespace CoreBank.Application.Accounts.Commands.CreateAccount;

public record CreateAccountCommand : IRequest<Result<CreateAccountResponse>>
{
    public Guid UserId { get; init; }
    public AccountType AccountType { get; init; }
    public string Currency { get; init; } = "USD";
}

public record CreateAccountResponse
{
    public Guid AccountId { get; init; }
    public string AccountNumber { get; init; } = null!;
    public AccountType AccountType { get; init; }
    public string Currency { get; init; } = null!;
    public decimal Balance { get; init; }
    public decimal DailyWithdrawalLimit { get; init; }
    public decimal InterestRate { get; init; }
}
