using CoreBank.Application.Common.Interfaces;
using CoreBank.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Infrastructure.Services;

public class TransactionLimitService : ITransactionLimitService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public TransactionLimitService(
        IApplicationDbContext context,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<TransactionLimitResult> CheckLimitAsync(
        Guid accountId,
        Guid userId,
        TransactionType transactionType,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account == null)
            return new TransactionLimitResult { IsAllowed = false, ViolatedLimit = "Account not found" };

        // Get applicable limits (user-specific, account-type specific, or global)
        var limits = await _context.TransactionLimits
            .Where(l => l.IsActive &&
                (l.UserId == null || l.UserId == userId) &&
                (l.AccountType == null || l.AccountType == account.AccountType) &&
                (l.TransactionType == null || l.TransactionType == transactionType))
            .ToListAsync(cancellationToken);

        var now = _dateTimeService.UtcNow;

        foreach (var limit in limits)
        {
            var (startDate, endDate) = GetPeriodDates(limit.LimitType, now);

            // Calculate current usage in this period
            var currentUsage = await CalculateUsageAsync(
                accountId,
                transactionType,
                startDate,
                endDate,
                cancellationToken);

            // Check per-transaction limit
            if (limit.LimitType == "PerTransaction" && amount > limit.LimitAmount)
            {
                return new TransactionLimitResult
                {
                    IsAllowed = false,
                    ViolatedLimit = $"Per-transaction limit ({limit.LimitType})",
                    LimitAmount = limit.LimitAmount,
                    CurrentUsage = amount,
                    RemainingAmount = limit.LimitAmount
                };
            }

            // Check periodic limits
            if (limit.LimitType != "PerTransaction")
            {
                var projectedUsage = currentUsage + amount;
                if (projectedUsage > limit.LimitAmount)
                {
                    return new TransactionLimitResult
                    {
                        IsAllowed = false,
                        ViolatedLimit = $"{limit.LimitType} limit exceeded",
                        LimitAmount = limit.LimitAmount,
                        CurrentUsage = currentUsage,
                        RemainingAmount = Math.Max(0, limit.LimitAmount - currentUsage)
                    };
                }
            }
        }

        // Check account-specific daily withdrawal limit for withdrawals
        if (transactionType == TransactionType.Withdrawal)
        {
            var todayStart = now.Date;
            var todayEnd = todayStart.AddDays(1);

            var dailyWithdrawals = await CalculateUsageAsync(
                accountId,
                TransactionType.Withdrawal,
                todayStart,
                todayEnd,
                cancellationToken);

            if (dailyWithdrawals + amount > account.DailyWithdrawalLimit)
            {
                return new TransactionLimitResult
                {
                    IsAllowed = false,
                    ViolatedLimit = "Daily withdrawal limit exceeded",
                    LimitAmount = account.DailyWithdrawalLimit,
                    CurrentUsage = dailyWithdrawals,
                    RemainingAmount = Math.Max(0, account.DailyWithdrawalLimit - dailyWithdrawals)
                };
            }
        }

        return new TransactionLimitResult { IsAllowed = true };
    }

    private async Task<decimal> CalculateUsageAsync(
        Guid accountId,
        TransactionType transactionType,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .Where(t =>
                t.Status == TransactionStatus.Completed &&
                t.Type == transactionType &&
                t.CreatedAt >= startDate &&
                t.CreatedAt < endDate &&
                (t.SourceAccountId == accountId || t.DestinationAccountId == accountId))
            .SumAsync(t => t.Amount, cancellationToken);
    }

    private static (DateTime startDate, DateTime endDate) GetPeriodDates(string limitType, DateTime now)
    {
        return limitType.ToLower() switch
        {
            "daily" => (now.Date, now.Date.AddDays(1)),
            "weekly" => (now.Date.AddDays(-(int)now.DayOfWeek), now.Date.AddDays(7 - (int)now.DayOfWeek)),
            "monthly" => (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, 1).AddMonths(1)),
            _ => (now.Date, now.Date.AddDays(1))
        };
    }
}
