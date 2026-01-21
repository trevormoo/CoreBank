using CoreBank.Application.Common.Models;
using CoreBank.Domain.Enums;
using MediatR;

namespace CoreBank.Application.Transactions.Commands.Deposit;

public record DepositCommand : IRequest<Result<TransactionResponse>>
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string? Description { get; init; }
    public string? IdempotencyKey { get; init; }
}

public record TransactionResponse
{
    public Guid TransactionId { get; init; }
    public string ReferenceNumber { get; init; } = null!;
    public TransactionType Type { get; init; }
    public TransactionStatus Status { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = null!;
    public decimal? NewBalance { get; init; }
    public DateTime Timestamp { get; init; }
}
