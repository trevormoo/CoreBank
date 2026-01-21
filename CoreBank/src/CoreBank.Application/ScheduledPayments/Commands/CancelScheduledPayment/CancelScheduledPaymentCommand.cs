using CoreBank.Application.Common.Models;
using MediatR;

namespace CoreBank.Application.ScheduledPayments.Commands.CancelScheduledPayment;

public record CancelScheduledPaymentCommand : IRequest<Result<CancelScheduledPaymentResponse>>
{
    public Guid ScheduledPaymentId { get; init; }
    public Guid RequestingUserId { get; init; }
}

public record CancelScheduledPaymentResponse
{
    public string Message { get; init; } = null!;
}
