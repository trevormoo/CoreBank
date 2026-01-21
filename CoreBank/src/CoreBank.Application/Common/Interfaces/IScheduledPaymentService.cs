namespace CoreBank.Application.Common.Interfaces;

public interface IScheduledPaymentService
{
    Task ProcessDuePaymentsAsync(CancellationToken cancellationToken = default);
    Task ProcessPaymentAsync(Guid scheduledPaymentId, CancellationToken cancellationToken = default);
}
