using CoreBank.Application.Common.Interfaces;
using CoreBank.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Infrastructure.Services;

public class FraudDetectionService : IFraudDetectionService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    // Configurable thresholds
    private const decimal HighValueThreshold = 10000m;
    private const decimal VeryHighValueThreshold = 50000m;
    private const int MaxTransactionsPerHour = 10;
    private const int MaxTransactionsPerDay = 50;
    private const decimal DailyVolumeThreshold = 100000m;

    public FraudDetectionService(
        IApplicationDbContext context,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<FraudCheckResult> CheckTransactionAsync(
        Guid accountId,
        Guid? userId,
        TransactionType transactionType,
        decimal amount,
        string? destinationAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var flags = new List<string>();
        var riskLevel = FraudRiskLevel.Low;
        var now = _dateTimeService.UtcNow;

        // 1. Check for high-value transactions
        if (amount >= VeryHighValueThreshold)
        {
            flags.Add("Very high value transaction");
            riskLevel = FraudRiskLevel.High;
        }
        else if (amount >= HighValueThreshold)
        {
            flags.Add("High value transaction");
            riskLevel = MaxRiskLevel(riskLevel, FraudRiskLevel.Medium);
        }

        // 2. Check transaction velocity (transactions per hour)
        var hourAgo = now.AddHours(-1);
        var transactionsLastHour = await _context.Transactions
            .CountAsync(t =>
                (t.SourceAccountId == accountId || t.DestinationAccountId == accountId) &&
                t.CreatedAt >= hourAgo,
                cancellationToken);

        if (transactionsLastHour >= MaxTransactionsPerHour)
        {
            flags.Add($"High transaction velocity: {transactionsLastHour} transactions in the last hour");
            riskLevel = MaxRiskLevel(riskLevel, FraudRiskLevel.High);
        }

        // 3. Check daily transaction count
        var todayStart = now.Date;
        var transactionsToday = await _context.Transactions
            .CountAsync(t =>
                (t.SourceAccountId == accountId || t.DestinationAccountId == accountId) &&
                t.CreatedAt >= todayStart,
                cancellationToken);

        if (transactionsToday >= MaxTransactionsPerDay)
        {
            flags.Add($"Daily transaction limit reached: {transactionsToday} transactions today");
            riskLevel = MaxRiskLevel(riskLevel, FraudRiskLevel.Medium);
        }

        // 4. Check daily volume
        var dailyVolume = await _context.Transactions
            .Where(t =>
                t.SourceAccountId == accountId &&
                t.Status == TransactionStatus.Completed &&
                t.CreatedAt >= todayStart)
            .SumAsync(t => t.Amount, cancellationToken);

        if (dailyVolume + amount > DailyVolumeThreshold)
        {
            flags.Add($"High daily volume: {dailyVolume + amount:C}");
            riskLevel = MaxRiskLevel(riskLevel, FraudRiskLevel.High);
        }

        // 5. Check for new account activity
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account != null && account.CreatedAt > now.AddDays(-7))
        {
            if (amount > 5000)
            {
                flags.Add("High value transaction on new account (< 7 days old)");
                riskLevel = MaxRiskLevel(riskLevel, FraudRiskLevel.High);
            }
        }

        // 6. Check for unusual timing (late night transactions)
        if (now.Hour >= 0 && now.Hour < 6)
        {
            if (amount > 1000)
            {
                flags.Add("High value transaction during unusual hours (midnight to 6 AM)");
                riskLevel = MaxRiskLevel(riskLevel, FraudRiskLevel.Medium);
            }
        }

        // 7. Check for rapid consecutive transfers to same destination
        if (transactionType == TransactionType.Transfer && !string.IsNullOrEmpty(destinationAccountId))
        {
            var recentToSameDestination = await _context.Transactions
                .CountAsync(t =>
                    t.SourceAccountId == accountId &&
                    t.DestinationAccountId.ToString() == destinationAccountId &&
                    t.Type == TransactionType.Transfer &&
                    t.CreatedAt >= now.AddMinutes(-30),
                    cancellationToken);

            if (recentToSameDestination >= 3)
            {
                flags.Add($"Multiple rapid transfers to same destination: {recentToSameDestination} in 30 minutes");
                riskLevel = MaxRiskLevel(riskLevel, FraudRiskLevel.High);
            }
        }

        // 8. Check for round-number transactions (potential structuring)
        if (amount >= 1000 && amount % 1000 == 0)
        {
            var recentRoundTransactions = await _context.Transactions
                .CountAsync(t =>
                    t.SourceAccountId == accountId &&
                    t.Amount >= 1000 &&
                    t.Amount % 1000 == 0 &&
                    t.CreatedAt >= now.AddHours(-24),
                    cancellationToken);

            if (recentRoundTransactions >= 3)
            {
                flags.Add("Potential structuring: multiple round-number transactions");
                riskLevel = MaxRiskLevel(riskLevel, FraudRiskLevel.High);
            }
        }

        // Determine if transaction should be blocked or require review
        var isAllowed = riskLevel != FraudRiskLevel.Critical;
        var requiresManualReview = riskLevel >= FraudRiskLevel.High;
        string? blockReason = null;

        if (!isAllowed)
        {
            blockReason = "Transaction blocked due to critical fraud risk indicators";
        }

        return new FraudCheckResult
        {
            IsAllowed = isAllowed,
            RiskLevel = riskLevel,
            Flags = flags,
            BlockReason = blockReason,
            RequiresManualReview = requiresManualReview
        };
    }

    private static FraudRiskLevel MaxRiskLevel(FraudRiskLevel current, FraudRiskLevel newLevel)
    {
        return (FraudRiskLevel)Math.Max((int)current, (int)newLevel);
    }
}
