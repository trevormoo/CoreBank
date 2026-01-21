using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.ScheduledPayments.Commands.CancelScheduledPayment;

public class CancelScheduledPaymentCommandHandler
    : IRequestHandler<CancelScheduledPaymentCommand, Result<CancelScheduledPaymentResponse>>
{
    private readonly IApplicationDbContext _context;

    public CancelScheduledPaymentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CancelScheduledPaymentResponse>> Handle(
        CancelScheduledPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var scheduledPayment = await _context.ScheduledPayments
            .Include(sp => sp.SourceAccount)
            .FirstOrDefaultAsync(sp => sp.Id == request.ScheduledPaymentId && !sp.IsDeleted, cancellationToken);

        if (scheduledPayment is null)
            return Result.Failure<CancelScheduledPaymentResponse>("Scheduled payment not found", "NOT_FOUND");

        // Check authorization
        if (scheduledPayment.SourceAccount.UserId != request.RequestingUserId)
            return Result.Failure<CancelScheduledPaymentResponse>("Access denied", "ACCESS_DENIED");

        // Soft delete
        scheduledPayment.IsDeleted = true;
        scheduledPayment.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new CancelScheduledPaymentResponse
        {
            Message = "Scheduled payment cancelled successfully"
        };
    }
}
