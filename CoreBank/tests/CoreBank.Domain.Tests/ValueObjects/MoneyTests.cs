using CoreBank.Domain.Exceptions;
using CoreBank.Domain.ValueObjects;
using FluentAssertions;

namespace CoreBank.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmount_ShouldCreateMoney()
    {
        // Act
        var money = Money.Create(100.50m, "USD");

        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrowDomainException()
    {
        // Act & Assert
        var act = () => Money.Create(-100m, "USD");
        act.Should().Throw<DomainException>()
            .WithMessage("Amount cannot be negative");
    }

    [Fact]
    public void Create_WithInvalidCurrency_ShouldThrowDomainException()
    {
        // Act & Assert
        var act = () => Money.Create(100m, "US");
        act.Should().Throw<DomainException>()
            .WithMessage("Currency must be a 3-letter ISO code");
    }

    [Fact]
    public void Create_ShouldRoundToTwoDecimals()
    {
        // Act
        var money = Money.Create(100.555m, "USD");

        // Assert
        money.Amount.Should().Be(100.56m);
    }

    [Fact]
    public void Add_WithSameCurrency_ShouldReturnSum()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(50m, "USD");

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_WithDifferentCurrency_ShouldThrowDomainException()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(50m, "EUR");

        // Act & Assert
        var act = () => money1.Add(money2);
        act.Should().Throw<DomainException>()
            .WithMessage("Cannot perform operation on different currencies: USD and EUR");
    }

    [Fact]
    public void Subtract_WithSufficientFunds_ShouldReturnDifference()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(50m, "USD");

        // Act
        var result = money1.Subtract(money2);

        // Assert
        result.Amount.Should().Be(50m);
    }

    [Fact]
    public void Subtract_WithInsufficientFunds_ShouldThrowInsufficientFundsException()
    {
        // Arrange
        var money1 = Money.Create(50m, "USD");
        var money2 = Money.Create(100m, "USD");

        // Act & Assert
        var act = () => money1.Subtract(money2);
        act.Should().Throw<InsufficientFundsException>();
    }

    [Fact]
    public void Zero_ShouldCreateZeroMoney()
    {
        // Act
        var money = Money.Zero("USD");

        // Assert
        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("USD");
        money.IsZero.Should().BeTrue();
    }

    [Fact]
    public void IsGreaterThan_ShouldCompareCorrectly()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(50m, "USD");

        // Assert
        money1.IsGreaterThan(money2).Should().BeTrue();
        money2.IsGreaterThan(money1).Should().BeFalse();
    }
}
