using CoreBank.Domain.Common;
using CoreBank.Domain.Enums;

namespace CoreBank.Domain.Entities;

public class ScheduledPayment : AuditableEntity, ISoftDelete
{
    public Guid SourceAccountId { get; private set; }
    public Guid DestinationAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string? Description { get; private set; }
    public string Frequency { get; private set; } = null!;  // Daily, Weekly, Monthly
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime? NextExecutionDate { get; private set; }
    public DateTime? LastExecutionDate { get; private set; }
    public int ExecutionCount { get; private set; }
    public int? MaxExecutions { get; private set; }
    public bool IsActive { get; private set; }
    public string? LastError { get; private set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public virtual Account SourceAccount { get; private set; } = null!;
    public virtual Account DestinationAccount { get; private set; } = null!;

    private ScheduledPayment() { }

    public static ScheduledPayment Create(
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        string currency,
        string frequency,
        DateTime startDate,
        DateTime? endDate = null,
        int? maxExecutions = null,
        string? description = null)
    {
        return new ScheduledPayment
        {
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            Amount = amount,
            Currency = currency,
            Description = description,
            Frequency = frequency,
            StartDate = startDate,
            EndDate = endDate,
            NextExecutionDate = startDate,
            MaxExecutions = maxExecutions,
            IsActive = true,
            ExecutionCount = 0
        };
    }

    public void RecordExecution()
    {
        LastExecutionDate = DateTime.UtcNow;
        ExecutionCount++;
        NextExecutionDate = CalculateNextExecutionDate();
        LastError = null;

        // Deactivate if max executions reached or past end date
        if ((MaxExecutions.HasValue && ExecutionCount >= MaxExecutions.Value) ||
            (EndDate.HasValue && NextExecutionDate > EndDate))
        {
            IsActive = false;
            NextExecutionDate = null;
        }
    }

    public void RecordFailure(string error)
    {
        LastError = error;
    }

    private DateTime? CalculateNextExecutionDate()
    {
        var baseDate = LastExecutionDate ?? StartDate;

        return Frequency.ToLower() switch
        {
            "daily" => baseDate.AddDays(1),
            "weekly" => baseDate.AddDays(7),
            "monthly" => baseDate.AddMonths(1),
            _ => baseDate.AddDays(1)
        };
    }

    public void Pause() => IsActive = false;
    public void Resume()
    {
        IsActive = true;
        if (NextExecutionDate < DateTime.UtcNow)
            NextExecutionDate = DateTime.UtcNow;
    }
}
