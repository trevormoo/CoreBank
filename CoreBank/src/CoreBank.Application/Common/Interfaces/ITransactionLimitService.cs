using CoreBank.Domain.Enums;

namespace CoreBank.Application.Common.Interfaces;

public interface ITransactionLimitService
{
    Task<TransactionLimitResult> CheckLimitAsync(
        Guid accountId,
        Guid userId,
        TransactionType transactionType,
        decimal amount,
        CancellationToken cancellationToken = default);
}

public record TransactionLimitResult
{
    public bool IsAllowed { get; init; }
    public string? ViolatedLimit { get; init; }
    public decimal? LimitAmount { get; init; }
    public decimal? CurrentUsage { get; init; }
    public decimal? RemainingAmount { get; init; }
}
