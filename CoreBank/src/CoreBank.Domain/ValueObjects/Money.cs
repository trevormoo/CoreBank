using CoreBank.Domain.Exceptions;

namespace CoreBank.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new DomainException("Amount cannot be negative");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required");

        if (currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO code");

        return new Money(Math.Round(amount, 2), currency);
    }

    public static Money Zero(string currency = "USD") => Create(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return Create(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        var result = Amount - other.Amount;
        if (result < 0)
            throw new InsufficientFundsException(other.Amount, Amount);
        return Create(result, Currency);
    }

    public bool IsGreaterThan(Money other)
    {
        EnsureSameCurrency(other);
        return Amount > other.Amount;
    }

    public bool IsLessThan(Money other)
    {
        EnsureSameCurrency(other);
        return Amount < other.Amount;
    }

    public bool IsZero => Amount == 0;

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot perform operation on different currencies: {Currency} and {other.Currency}");
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
