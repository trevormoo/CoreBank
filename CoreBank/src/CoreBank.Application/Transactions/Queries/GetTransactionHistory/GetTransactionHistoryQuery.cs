using CoreBank.Application.Common.Models;
using CoreBank.Domain.Enums;
using MediatR;

namespace CoreBank.Application.Transactions.Queries.GetTransactionHistory;

public record GetTransactionHistoryQuery : IRequest<Result<PaginatedList<TransactionDto>>>
{
    public Guid AccountId { get; init; }
    public Guid? RequestingUserId { get; init; }
    public TransactionType? Type { get; init; }
    public TransactionStatus? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record TransactionDto
{
    public Guid Id { get; init; }
    public string ReferenceNumber { get; init; } = null!;
    public TransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = null!;
    public TransactionStatus Status { get; init; }
    public string? Description { get; init; }
    public Guid? SourceAccountId { get; init; }
    public string? SourceAccountNumber { get; init; }
    public Guid? DestinationAccountId { get; init; }
    public string? DestinationAccountNumber { get; init; }
    public decimal? BalanceAfter { get; init; }
    public string? FailureReason { get; init; }
    public DateTime CreatedAt { get; init; }
}
