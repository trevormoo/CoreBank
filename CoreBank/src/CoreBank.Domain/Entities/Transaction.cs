using CoreBank.Domain.Common;
using CoreBank.Domain.Enums;
using CoreBank.Domain.Exceptions;
using CoreBank.Domain.ValueObjects;

namespace CoreBank.Domain.Entities;

public class Transaction : AuditableEntity
{
    public string ReferenceNumber { get; private set; } = null!;
    public Guid? SourceAccountId { get; private set; }
    public Guid? DestinationAccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public TransactionStatus Status { get; private set; }
    public string? Description { get; private set; }
    public string? FailureReason { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public decimal? BalanceAfterSource { get; private set; }
    public decimal? BalanceAfterDestination { get; private set; }

    // For reversals
    public Guid? OriginalTransactionId { get; private set; }
    public virtual Transaction? OriginalTransaction { get; private set; }
    public virtual Transaction? ReversalTransaction { get; private set; }

    // Navigation properties
    public virtual Account? SourceAccount { get; private set; }
    public virtual Account? DestinationAccount { get; private set; }

    private Transaction() { }

    public static Transaction CreateDeposit(
        Guid destinationAccountId,
        Money amount,
        string? description = null,
        string? idempotencyKey = null)
    {
        return new Transaction
        {
            ReferenceNumber = GenerateReferenceNumber(),
            DestinationAccountId = destinationAccountId,
            Type = TransactionType.Deposit,
            Amount = amount.Amount,
            Currency = amount.Currency,
            Status = TransactionStatus.Pending,
            Description = description ?? "Deposit",
            IdempotencyKey = idempotencyKey
        };
    }

    public static Transaction CreateWithdrawal(
        Guid sourceAccountId,
        Money amount,
        string? description = null,
        string? idempotencyKey = null)
    {
        return new Transaction
        {
            ReferenceNumber = GenerateReferenceNumber(),
            SourceAccountId = sourceAccountId,
            Type = TransactionType.Withdrawal,
            Amount = amount.Amount,
            Currency = amount.Currency,
            Status = TransactionStatus.Pending,
            Description = description ?? "Withdrawal",
            IdempotencyKey = idempotencyKey
        };
    }

    public static Transaction CreateTransfer(
        Guid sourceAccountId,
        Guid destinationAccountId,
        Money amount,
        string? description = null,
        string? idempotencyKey = null)
    {
        if (sourceAccountId == destinationAccountId)
            throw new DomainException("Cannot transfer to the same account");

        return new Transaction
        {
            ReferenceNumber = GenerateReferenceNumber(),
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            Type = TransactionType.Transfer,
            Amount = amount.Amount,
            Currency = amount.Currency,
            Status = TransactionStatus.Pending,
            Description = description ?? "Transfer",
            IdempotencyKey = idempotencyKey
        };
    }

    public static Transaction CreateReversal(
        Transaction originalTransaction,
        string? description = null)
    {
        if (originalTransaction.Status != TransactionStatus.Completed)
            throw new DomainException("Can only reverse completed transactions");

        if (originalTransaction.Type == TransactionType.Reversal)
            throw new DomainException("Cannot reverse a reversal");

        return new Transaction
        {
            ReferenceNumber = GenerateReferenceNumber(),
            // Swap source and destination for reversal
            SourceAccountId = originalTransaction.DestinationAccountId,
            DestinationAccountId = originalTransaction.SourceAccountId,
            Type = TransactionType.Reversal,
            Amount = originalTransaction.Amount,
            Currency = originalTransaction.Currency,
            Status = TransactionStatus.Pending,
            Description = description ?? $"Reversal of {originalTransaction.ReferenceNumber}",
            OriginalTransactionId = originalTransaction.Id
        };
    }

    private static string GenerateReferenceNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(100000, 999999);
        return $"TXN{timestamp}{random}";
    }

    public void MarkCompleted(decimal? sourceBalanceAfter = null, decimal? destBalanceAfter = null)
    {
        if (Status != TransactionStatus.Pending)
            throw new DomainException($"Cannot complete a transaction with status {Status}");

        Status = TransactionStatus.Completed;
        BalanceAfterSource = sourceBalanceAfter;
        BalanceAfterDestination = destBalanceAfter;
    }

    public void MarkFailed(string reason)
    {
        if (Status != TransactionStatus.Pending)
            throw new DomainException($"Cannot fail a transaction with status {Status}");

        Status = TransactionStatus.Failed;
        FailureReason = reason;
    }

    public void MarkReversed()
    {
        if (Status != TransactionStatus.Completed)
            throw new DomainException("Can only reverse completed transactions");

        Status = TransactionStatus.Reversed;
    }

    public Money GetMoney() => Money.Create(Amount, Currency);
}
