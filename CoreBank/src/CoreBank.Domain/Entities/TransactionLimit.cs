using CoreBank.Domain.Common;
using CoreBank.Domain.Enums;

namespace CoreBank.Domain.Entities;

public class TransactionLimit : AuditableEntity
{
    public Guid? UserId { get; private set; }         // null = global limit
    public AccountType? AccountType { get; private set; }  // null = all account types
    public TransactionType? TransactionType { get; private set; } // null = all transaction types
    public string LimitType { get; private set; } = null!;  // Daily, Weekly, Monthly, PerTransaction
    public decimal LimitAmount { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation
    public virtual User? User { get; private set; }

    private TransactionLimit() { }

    public static TransactionLimit Create(
        string limitType,
        decimal limitAmount,
        Guid? userId = null,
        AccountType? accountType = null,
        TransactionType? transactionType = null)
    {
        return new TransactionLimit
        {
            UserId = userId,
            AccountType = accountType,
            TransactionType = transactionType,
            LimitType = limitType,
            LimitAmount = limitAmount,
            IsActive = true
        };
    }

    public void UpdateLimit(decimal newAmount)
    {
        LimitAmount = newAmount;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
