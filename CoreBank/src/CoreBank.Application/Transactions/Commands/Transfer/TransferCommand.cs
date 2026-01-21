using CoreBank.Application.Common.Models;
using CoreBank.Application.Transactions.Commands.Deposit;
using MediatR;

namespace CoreBank.Application.Transactions.Commands.Transfer;

public record TransferCommand : IRequest<Result<TransferResponse>>
{
    public Guid SourceAccountId { get; init; }
    public Guid DestinationAccountId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string? Description { get; init; }
    public string? IdempotencyKey { get; init; }
}

public record TransferResponse : TransactionResponse
{
    public decimal? SourceNewBalance { get; init; }
    public decimal? DestinationNewBalance { get; init; }
}
