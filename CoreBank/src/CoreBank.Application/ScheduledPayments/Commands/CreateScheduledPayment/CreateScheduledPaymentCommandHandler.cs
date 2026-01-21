using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using CoreBank.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.ScheduledPayments.Commands.CreateScheduledPayment;

public class CreateScheduledPaymentCommandHandler
    : IRequestHandler<CreateScheduledPaymentCommand, Result<CreateScheduledPaymentResponse>>
{
    private readonly IApplicationDbContext _context;

    public CreateScheduledPaymentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CreateScheduledPaymentResponse>> Handle(
        CreateScheduledPaymentCommand request,
        CancellationToken cancellationToken)
    {
        // Verify source account exists and user has access
        var sourceAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.SourceAccountId && !a.IsDeleted, cancellationToken);

        if (sourceAccount is null)
            return Result.Failure<CreateScheduledPaymentResponse>("Source account not found", "SOURCE_ACCOUNT_NOT_FOUND");

        if (sourceAccount.UserId != request.RequestingUserId)
            return Result.Failure<CreateScheduledPaymentResponse>("Access denied", "ACCESS_DENIED");

        // Verify destination account exists
        var destinationAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.DestinationAccountId && !a.IsDeleted, cancellationToken);

        if (destinationAccount is null)
            return Result.Failure<CreateScheduledPaymentResponse>("Destination account not found", "DESTINATION_ACCOUNT_NOT_FOUND");

        // Cannot schedule to same account
        if (request.SourceAccountId == request.DestinationAccountId)
            return Result.Failure<CreateScheduledPaymentResponse>(
                "Cannot schedule payment to the same account",
                "SAME_ACCOUNT");

        // Validate currencies match
        if (sourceAccount.Currency != destinationAccount.Currency)
            return Result.Failure<CreateScheduledPaymentResponse>(
                "Cannot schedule payment between accounts with different currencies",
                "CURRENCY_MISMATCH");

        // Create scheduled payment
        var scheduledPayment = ScheduledPayment.Create(
            request.SourceAccountId,
            request.DestinationAccountId,
            request.Amount,
            request.Currency,
            request.Frequency,
            request.StartDate,
            request.EndDate,
            request.MaxExecutions,
            request.Description);

        _context.ScheduledPayments.Add(scheduledPayment);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateScheduledPaymentResponse
        {
            ScheduledPaymentId = scheduledPayment.Id,
            Frequency = scheduledPayment.Frequency,
            NextExecutionDate = scheduledPayment.NextExecutionDate!.Value,
            Message = "Scheduled payment created successfully"
        };
    }
}
