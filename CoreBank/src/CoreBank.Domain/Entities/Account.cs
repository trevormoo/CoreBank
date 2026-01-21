using CoreBank.Domain.Common;
using CoreBank.Domain.Enums;
using CoreBank.Domain.Exceptions;
using CoreBank.Domain.ValueObjects;

namespace CoreBank.Domain.Entities;

public class Account : AuditableEntity, ISoftDelete
{
    public string AccountNumber { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public AccountType AccountType { get; private set; }
    public decimal Balance { get; private set; }
    public string Currency { get; private set; } = "USD";
    public AccountStatus Status { get; private set; }
    public decimal DailyWithdrawalLimit { get; private set; }
    public decimal DailyWithdrawalUsed { get; private set; }
    public DateTime DailyLimitResetDate { get; private set; }
    public decimal InterestRate { get; private set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public virtual User User { get; private set; } = null!;
    public virtual ICollection<Transaction> SourceTransactions { get; private set; } = new List<Transaction>();
    public virtual ICollection<Transaction> DestinationTransactions { get; private set; } = new List<Transaction>();

    private Account() { }

    public static Account Create(
        Guid userId,
        AccountType accountType,
        string currency = "USD")
    {
        var (limit, interestRate) = GetAccountTypeDefaults(accountType);

        var account = new Account
        {
            UserId = userId,
            AccountNumber = GenerateAccountNumber(),
            AccountType = accountType,
            Balance = 0,
            Currency = currency.ToUpperInvariant(),
            Status = AccountStatus.Active,
            DailyWithdrawalLimit = limit,
            DailyWithdrawalUsed = 0,
            DailyLimitResetDate = DateTime.UtcNow.Date,
            InterestRate = interestRate
        };

        return account;
    }

    private static (decimal limit, decimal interestRate) GetAccountTypeDefaults(AccountType accountType)
    {
        return accountType switch
        {
            AccountType.Savings => (5000m, 0.02m),      // $5,000 daily limit, 2% interest
            AccountType.Current => (50000m, 0m),        // $50,000 daily limit, no interest
            AccountType.FixedDeposit => (0m, 0.05m),    // No withdrawals, 5% interest
            _ => throw new DomainException($"Unknown account type: {accountType}")
        };
    }

    private static string GenerateAccountNumber()
    {
        // Generate a 10-digit account number
        var random = new Random();
        var number = "";
        for (int i = 0; i < 9; i++)
        {
            number += random.Next(0, 10).ToString();
        }

        // Add check digit using Luhn algorithm
        var checkDigit = CalculateLuhnCheckDigit(number);
        return number + checkDigit;
    }

    private static int CalculateLuhnCheckDigit(string number)
    {
        var sum = 0;
        var alternate = true;

        for (int i = number.Length - 1; i >= 0; i--)
        {
            var digit = int.Parse(number[i].ToString());

            if (alternate)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
            alternate = !alternate;
        }

        return (10 - (sum % 10)) % 10;
    }

    public void EnsureCanTransact()
    {
        if (Status == AccountStatus.Frozen)
            throw new AccountFrozenException(AccountNumber);

        if (Status == AccountStatus.Closed)
            throw new DomainException($"Account {AccountNumber} is closed");
    }

    public void Deposit(Money amount)
    {
        EnsureCanTransact();

        if (amount.Currency != Currency)
            throw new DomainException($"Cannot deposit {amount.Currency} to a {Currency} account");

        Balance += amount.Amount;
    }

    public void Withdraw(Money amount)
    {
        EnsureCanTransact();

        if (amount.Currency != Currency)
            throw new DomainException($"Cannot withdraw {amount.Currency} from a {Currency} account");

        if (AccountType == AccountType.FixedDeposit)
            throw new DomainException("Cannot withdraw from a fixed deposit account");

        // Reset daily limit if it's a new day
        ResetDailyLimitIfNeeded();

        // Check daily withdrawal limit
        if (DailyWithdrawalUsed + amount.Amount > DailyWithdrawalLimit)
            throw new TransactionLimitExceededException(
                amount.Amount,
                DailyWithdrawalLimit - DailyWithdrawalUsed,
                "Daily withdrawal");

        if (amount.Amount > Balance)
            throw new InsufficientFundsException(amount.Amount, Balance);

        Balance -= amount.Amount;
        DailyWithdrawalUsed += amount.Amount;
    }

    private void ResetDailyLimitIfNeeded()
    {
        if (DateTime.UtcNow.Date > DailyLimitResetDate)
        {
            DailyWithdrawalUsed = 0;
            DailyLimitResetDate = DateTime.UtcNow.Date;
        }
    }

    public void Freeze()
    {
        if (Status == AccountStatus.Closed)
            throw new DomainException("Cannot freeze a closed account");

        Status = AccountStatus.Frozen;
    }

    public void Unfreeze()
    {
        if (Status != AccountStatus.Frozen)
            throw new DomainException("Account is not frozen");

        Status = AccountStatus.Active;
    }

    public void Close()
    {
        if (Balance > 0)
            throw new DomainException("Cannot close an account with positive balance. Please withdraw all funds first.");

        Status = AccountStatus.Closed;
    }

    public void SetDailyWithdrawalLimit(decimal limit)
    {
        if (limit < 0)
            throw new DomainException("Daily withdrawal limit cannot be negative");

        DailyWithdrawalLimit = limit;
    }

    public Money GetBalance() => Money.Create(Balance, Currency);
}
