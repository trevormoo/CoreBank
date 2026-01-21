using CoreBank.Application.Common.Models;
using CoreBank.Application.Transactions.Commands.Deposit;
using MediatR;

namespace CoreBank.Application.Transactions.Commands.Withdraw;

public record WithdrawCommand : IRequest<Result<TransactionResponse>>
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string? Description { get; init; }
    public string? IdempotencyKey { get; init; }
}
