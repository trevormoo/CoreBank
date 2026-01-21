namespace CoreBank.Application.Common.Interfaces;

public interface IStatementService
{
    Task<byte[]> GenerateAccountStatementAsync(
        Guid accountId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}

public record StatementData
{
    public string AccountNumber { get; init; } = null!;
    public string AccountHolderName { get; init; } = null!;
    public string AccountType { get; init; } = null!;
    public string Currency { get; init; } = null!;
    public decimal OpeningBalance { get; init; }
    public decimal ClosingBalance { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public DateTime GeneratedAt { get; init; }
    public List<StatementTransaction> Transactions { get; init; } = new();
}

public record StatementTransaction
{
    public DateTime Date { get; init; }
    public string ReferenceNumber { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string Type { get; init; } = null!;
    public decimal? Debit { get; init; }
    public decimal? Credit { get; init; }
    public decimal Balance { get; init; }
}
