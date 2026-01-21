using CoreBank.Application.Common.Models;
using MediatR;

namespace CoreBank.Application.ScheduledPayments.Queries.GetUserScheduledPayments;

public record GetUserScheduledPaymentsQuery : IRequest<Result<List<ScheduledPaymentDto>>>
{
    public Guid UserId { get; init; }
    public bool? ActiveOnly { get; init; }
}

public record ScheduledPaymentDto
{
    public Guid Id { get; init; }
    public Guid SourceAccountId { get; init; }
    public string SourceAccountNumber { get; init; } = null!;
    public Guid DestinationAccountId { get; init; }
    public string DestinationAccountNumber { get; init; } = null!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = null!;
    public string? Description { get; init; }
    public string Frequency { get; init; } = null!;
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime? NextExecutionDate { get; init; }
    public DateTime? LastExecutionDate { get; init; }
    public int ExecutionCount { get; init; }
    public int? MaxExecutions { get; init; }
    public bool IsActive { get; init; }
    public string? LastError { get; init; }
    public DateTime CreatedAt { get; init; }
}
