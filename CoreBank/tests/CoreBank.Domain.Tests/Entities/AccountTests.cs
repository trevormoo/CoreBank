using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using CoreBank.Domain.Exceptions;
using CoreBank.Domain.ValueObjects;
using FluentAssertions;

namespace CoreBank.Domain.Tests.Entities;

public class AccountTests
{
    [Fact]
    public void Create_ShouldCreateAccountWithCorrectDefaults()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var account = Account.Create(userId, AccountType.Savings, "USD");

        // Assert
        account.UserId.Should().Be(userId);
        account.AccountType.Should().Be(AccountType.Savings);
        account.Currency.Should().Be("USD");
        account.Balance.Should().Be(0);
        account.Status.Should().Be(AccountStatus.Active);
        account.AccountNumber.Should().HaveLength(10);
        account.DailyWithdrawalLimit.Should().Be(5000m); // Savings default
        account.InterestRate.Should().Be(0.02m); // Savings default
    }

    [Fact]
    public void Create_CurrentAccount_ShouldHaveHigherWithdrawalLimit()
    {
        // Act
        var account = Account.Create(Guid.NewGuid(), AccountType.Current, "USD");

        // Assert
        account.DailyWithdrawalLimit.Should().Be(50000m);
        account.InterestRate.Should().Be(0m);
    }

    [Fact]
    public void Deposit_ShouldIncreaseBalance()
    {
        // Arrange
        var account = Account.Create(Guid.NewGuid(), AccountType.Savings, "USD");
        var amount = Money.Create(100m, "USD");

        // Act
        account.Deposit(amount);

        // Assert
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void Deposit_WhenAccountFrozen_ShouldThrowAccountFrozenException()
    {
        // Arrange
        var account = Account.Create(Guid.NewGuid(), AccountType.Savings, "USD");
        account.Freeze();
        var amount = Money.Create(100m, "USD");

        // Act & Assert
        var act = () => account.Deposit(amount);
        act.Should().Throw<AccountFrozenException>();
    }

    [Fact]
    public void Withdraw_WithSufficientBalance_ShouldDecreaseBalance()
    {
        // Arrange
        var account = Account.Create(Guid.NewGuid(), AccountType.Savings, "USD");
        account.Deposit(Money.Create(500m, "USD"));
        var withdrawAmount = Money.Create(200m, "USD");

        // Act
        account.Withdraw(withdrawAmount);

        // Assert
        account.Balance.Should().Be(300m);
    }

    [Fact]
    public void Withdraw_WithInsufficientBalance_ShouldThrowInsufficientFundsException()
    {
        // Arrange
        var account = Account.Create(Guid.NewGuid(), AccountType.Savings, "USD");
        account.Deposit(Money.Create(100m, "USD"));
        var withdrawAmount = Money.Create(200m, "USD");

        // Act & Assert
        var act = () => account.Withdraw(withdrawAmount);
        act.Should().Throw<InsufficientFundsException>();
    }

    [Fact]
    public void Withdraw_ExceedingDailyLimit_ShouldThrowTransactionLimitExceededException()
    {
        // Arrange
        var account = Account.Create(Guid.NewGuid(), AccountType.Savings, "USD");
        account.Deposit(Money.Create(10000m, "USD"));
        var withdrawAmount = Money.Create(6000m, "USD"); // Exceeds $5000 daily limit

        // Act & Assert
        var act = () => account.Withdraw(withdrawAmount);
        act.Should().Throw<TransactionLimitExceededException>();
    }

    [Fact]
    public void Withdraw_FromFixedDeposit_ShouldThrowDomainException()
    {
        // Arrange
        var account = Account.Create(Guid.NewGuid(), AccountType.FixedDeposit, "USD");
        account.Deposit(Money.Create(1000m, "USD"));
        var withdrawAmount = Money.Create(100m, "USD");

        // Act & Assert
        var act = () => account.Withdraw(withdrawAmount);
        act.Should().Throw<DomainException>()
            .WithMessage("Cannot withdraw from a fixed deposit account");
    }

    [Fact]
    public void Freeze_ShouldSetStatusToFrozen()
    {
        // Arrange
        var account = Account.Create(Guid.NewGuid(), AccountType.Savings, "USD");

        // Act
        account.Freeze();

        // Assert
        account.Status.Should().Be(AccountStatus.Frozen);
    }

    [Fact]
    public void Unfreeze_ShouldSetStatusToActive()
    {
        // Arrange
        var account = Account.Create(Guid.NewGuid(), AccountType.Savings, "USD");
        account.Freeze();

        // Act
        account.Unfreeze();

        // Assert
        account.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public void Close_WithPositiveBalance_ShouldThrowDomainException()
    {
        // Arrange
        var account = Account.Create(Guid.NewGuid(), AccountType.Savings, "USD");
        account.Deposit(Money.Create(100m, "USD"));

        // Act & Assert
        var act = () => account.Close();
        act.Should().Throw<DomainException>()
            .WithMessage("Cannot close an account with positive balance*");
    }
}
