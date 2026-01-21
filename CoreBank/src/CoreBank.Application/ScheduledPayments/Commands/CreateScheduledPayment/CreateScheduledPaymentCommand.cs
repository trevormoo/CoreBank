using CoreBank.Application.Common.Models;
using MediatR;

namespace CoreBank.Application.ScheduledPayments.Commands.CreateScheduledPayment;

public record CreateScheduledPaymentCommand : IRequest<Result<CreateScheduledPaymentResponse>>
{
    public Guid SourceAccountId { get; init; }
    public Guid DestinationAccountId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string Frequency { get; init; } = null!; // Daily, Weekly, Monthly
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int? MaxExecutions { get; init; }
    public string? Description { get; init; }
    public Guid RequestingUserId { get; init; }
}

public record CreateScheduledPaymentResponse
{
    public Guid ScheduledPaymentId { get; init; }
    public string Frequency { get; init; } = null!;
    public DateTime NextExecutionDate { get; init; }
    public string Message { get; init; } = null!;
}
