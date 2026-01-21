using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using CoreBank.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Transactions.Commands.Transfer;

public class TransferCommandHandler : IRequestHandler<TransferCommand, Result<TransferResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITransactionLimitService _limitService;
    private readonly IFraudDetectionService _fraudService;

    public TransferCommandHandler(
        IApplicationDbContext context,
        ITransactionLimitService limitService,
        IFraudDetectionService fraudService)
    {
        _context = context;
        _limitService = limitService;
        _fraudService = fraudService;
    }

    public async Task<Result<TransferResponse>> Handle(
        TransferCommand request,
        CancellationToken cancellationToken)
    {
        // Check for idempotency
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey, cancellationToken);

            if (existingTransaction is not null)
            {
                return new TransferResponse
                {
                    TransactionId = existingTransaction.Id,
                    ReferenceNumber = existingTransaction.ReferenceNumber,
                    Type = existingTransaction.Type,
                    Status = existingTransaction.Status,
                    Amount = existingTransaction.Amount,
                    Currency = existingTransaction.Currency,
                    SourceNewBalance = existingTransaction.BalanceAfterSource,
                    DestinationNewBalance = existingTransaction.BalanceAfterDestination,
                    Timestamp = existingTransaction.CreatedAt
                };
            }
        }

        // Get source account
        var sourceAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.SourceAccountId && !a.IsDeleted, cancellationToken);

        if (sourceAccount is null)
            return Result.Failure<TransferResponse>("Source account not found", "SOURCE_ACCOUNT_NOT_FOUND");

        // Get destination account
        var destinationAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.DestinationAccountId && !a.IsDeleted, cancellationToken);

        if (destinationAccount is null)
            return Result.Failure<TransferResponse>("Destination account not found", "DESTINATION_ACCOUNT_NOT_FOUND");

        // Validate currencies match
        if (sourceAccount.Currency != destinationAccount.Currency)
            return Result.Failure<TransferResponse>(
                "Cannot transfer between accounts with different currencies",
                "CURRENCY_MISMATCH");

        // Create money value object
        Money amount;
        try
        {
            amount = Money.Create(request.Amount, request.Currency);
        }
        catch (Exception ex)
        {
            return Result.Failure<TransferResponse>(ex.Message, "INVALID_AMOUNT");
        }

        // Validate currency matches accounts
        if (amount.Currency != sourceAccount.Currency)
            return Result.Failure<TransferResponse>(
                $"Transfer currency ({amount.Currency}) does not match account currency ({sourceAccount.Currency})",
                "CURRENCY_MISMATCH");

        // Check transaction limits
        var limitResult = await _limitService.CheckLimitAsync(
            request.SourceAccountId,
            sourceAccount.UserId,
            TransactionType.Transfer,
            request.Amount,
            cancellationToken);

        if (!limitResult.IsAllowed)
            return Result.Failure<TransferResponse>(
                $"{limitResult.ViolatedLimit}. Limit: {limitResult.LimitAmount:C}, Used: {limitResult.CurrentUsage:C}, Remaining: {limitResult.RemainingAmount:C}",
                "LIMIT_EXCEEDED");

        // Check for fraud
        var fraudResult = await _fraudService.CheckTransactionAsync(
            request.SourceAccountId,
            sourceAccount.UserId,
            TransactionType.Transfer,
            request.Amount,
            request.DestinationAccountId.ToString(),
            cancellationToken);

        if (!fraudResult.IsAllowed)
            return Result.Failure<TransferResponse>(
                fraudResult.BlockReason ?? "Transaction blocked by fraud detection",
                "FRAUD_DETECTED");

        // Create transaction
        var transaction = Transaction.CreateTransfer(
            request.SourceAccountId,
            request.DestinationAccountId,
            amount,
            request.Description,
            request.IdempotencyKey);

        try
        {
            // Perform transfer
            sourceAccount.Withdraw(amount);
            destinationAccount.Deposit(amount);
            transaction.MarkCompleted(
                sourceBalanceAfter: sourceAccount.Balance,
                destBalanceAfter: destinationAccount.Balance);
        }
        catch (Exception ex)
        {
            transaction.MarkFailed(ex.Message);
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Failure<TransferResponse>(ex.Message, "TRANSFER_FAILED");
        }

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        return new TransferResponse
        {
            TransactionId = transaction.Id,
            ReferenceNumber = transaction.ReferenceNumber,
            Type = transaction.Type,
            Status = transaction.Status,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            SourceNewBalance = sourceAccount.Balance,
            DestinationNewBalance = destinationAccount.Balance,
            Timestamp = transaction.CreatedAt
        };
    }
}
