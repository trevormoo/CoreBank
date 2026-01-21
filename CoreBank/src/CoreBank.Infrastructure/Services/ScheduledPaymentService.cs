using CoreBank.Application.Common.Interfaces;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using CoreBank.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreBank.Infrastructure.Services;

public class ScheduledPaymentService : IScheduledPaymentService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ScheduledPaymentService> _logger;

    public ScheduledPaymentService(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        IEmailService emailService,
        ILogger<ScheduledPaymentService> logger)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ProcessDuePaymentsAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeService.UtcNow;

        var duePayments = await _context.ScheduledPayments
            .Where(sp => sp.IsActive &&
                         !sp.IsDeleted &&
                         sp.NextExecutionDate != null &&
                         sp.NextExecutionDate <= now)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} scheduled payments due for processing", duePayments.Count);

        foreach (var payment in duePayments)
        {
            try
            {
                await ProcessPaymentAsync(payment.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled payment {PaymentId}", payment.Id);
            }
        }
    }

    public async Task ProcessPaymentAsync(Guid scheduledPaymentId, CancellationToken cancellationToken = default)
    {
        var scheduledPayment = await _context.ScheduledPayments
            .Include(sp => sp.SourceAccount)
                .ThenInclude(a => a.User)
            .Include(sp => sp.DestinationAccount)
            .FirstOrDefaultAsync(sp => sp.Id == scheduledPaymentId && !sp.IsDeleted, cancellationToken);

        if (scheduledPayment is null)
        {
            _logger.LogWarning("Scheduled payment {PaymentId} not found", scheduledPaymentId);
            return;
        }

        if (!scheduledPayment.IsActive)
        {
            _logger.LogInformation("Scheduled payment {PaymentId} is not active, skipping", scheduledPaymentId);
            return;
        }

        var sourceAccount = scheduledPayment.SourceAccount;
        var destinationAccount = scheduledPayment.DestinationAccount;

        _logger.LogInformation(
            "Processing scheduled payment {PaymentId}: {Amount} {Currency} from {SourceAccount} to {DestinationAccount}",
            scheduledPaymentId,
            scheduledPayment.Amount,
            scheduledPayment.Currency,
            sourceAccount.AccountNumber,
            destinationAccount.AccountNumber);

        try
        {
            // Create money value object
            var amount = Money.Create(scheduledPayment.Amount, scheduledPayment.Currency);

            // Create transaction
            var transaction = Transaction.CreateTransfer(
                scheduledPayment.SourceAccountId,
                scheduledPayment.DestinationAccountId,
                amount,
                scheduledPayment.Description ?? $"Scheduled payment - {scheduledPayment.Frequency}");

            // Perform transfer
            sourceAccount.Withdraw(amount);
            destinationAccount.Deposit(amount);
            transaction.MarkCompleted(
                sourceBalanceAfter: sourceAccount.Balance,
                destBalanceAfter: destinationAccount.Balance);

            _context.Transactions.Add(transaction);

            // Record successful execution
            scheduledPayment.RecordExecution();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Scheduled payment {PaymentId} executed successfully. Transaction: {TransactionRef}",
                scheduledPaymentId,
                transaction.ReferenceNumber);

            // Send notification
            await _emailService.SendTransactionReceiptAsync(
                sourceAccount.User.Email,
                transaction.ReferenceNumber,
                transaction.Amount,
                "Scheduled Transfer",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process scheduled payment {PaymentId}", scheduledPaymentId);

            scheduledPayment.RecordFailure(ex.Message);
            await _context.SaveChangesAsync(cancellationToken);

            // Notify user of failure
            await _emailService.SendAccountNotificationAsync(
                sourceAccount.User.Email,
                "Scheduled Payment Failed",
                $"Your scheduled payment of {scheduledPayment.Amount} {scheduledPayment.Currency} failed. Reason: {ex.Message}",
                cancellationToken);
        }
    }
}
