namespace CoreBank.Domain.Exceptions;

public class AccountFrozenException : DomainException
{
    public string AccountNumber { get; }

    public AccountFrozenException(string accountNumber)
        : base($"Account {accountNumber} is frozen and cannot perform transactions")
    {
        AccountNumber = accountNumber;
    }
}
