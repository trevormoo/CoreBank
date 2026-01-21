namespace CoreBank.Domain.Exceptions;

public class TransactionLimitExceededException : DomainException
{
    public decimal RequestedAmount { get; }
    public decimal Limit { get; }
    public string LimitType { get; }

    public TransactionLimitExceededException(decimal requestedAmount, decimal limit, string limitType)
        : base($"{limitType} limit exceeded. Requested: {requestedAmount:C}, Limit: {limit:C}")
    {
        RequestedAmount = requestedAmount;
        Limit = limit;
        LimitType = limitType;
    }
}
