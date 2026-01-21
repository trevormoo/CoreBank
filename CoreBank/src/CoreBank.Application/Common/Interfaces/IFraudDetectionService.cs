using CoreBank.Domain.Enums;

namespace CoreBank.Application.Common.Interfaces;

public interface IFraudDetectionService
{
    Task<FraudCheckResult> CheckTransactionAsync(
        Guid accountId,
        Guid? userId,
        TransactionType transactionType,
        decimal amount,
        string? destinationAccountId = null,
        CancellationToken cancellationToken = default);
}

public record FraudCheckResult
{
    public bool IsAllowed { get; init; }
    public FraudRiskLevel RiskLevel { get; init; }
    public List<string> Flags { get; init; } = new();
    public string? BlockReason { get; init; }
    public bool RequiresManualReview { get; init; }
}

public enum FraudRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
