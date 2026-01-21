using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using CoreBank.Application.Transactions.Commands.Deposit;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using CoreBank.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Transactions.Commands.Withdraw;

public class WithdrawCommandHandler : IRequestHandler<WithdrawCommand, Result<TransactionResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITransactionLimitService _limitService;
    private readonly IFraudDetectionService _fraudService;

    public WithdrawCommandHandler(
        IApplicationDbContext context,
        ITransactionLimitService limitService,
        IFraudDetectionService fraudService)
    {
        _context = context;
        _limitService = limitService;
        _fraudService = fraudService;
    }

    public async Task<Result<TransactionResponse>> Handle(
        WithdrawCommand request,
        CancellationToken cancellationToken)
    {
        // Check for idempotency
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey, cancellationToken);

            if (existingTransaction is not null)
            {
                return new TransactionResponse
                {
                    TransactionId = existingTransaction.Id,
                    ReferenceNumber = existingTransaction.ReferenceNumber,
                    Type = existingTransaction.Type,
                    Status = existingTransaction.Status,
                    Amount = existingTransaction.Amount,
                    Currency = existingTransaction.Currency,
                    NewBalance = existingTransaction.BalanceAfterSource,
                    Timestamp = existingTransaction.CreatedAt
                };
            }
        }

        // Get account
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && !a.IsDeleted, cancellationToken);

        if (account is null)
            return Result.Failure<TransactionResponse>("Account not found", "ACCOUNT_NOT_FOUND");

        // Create money value object
        Money amount;
        try
        {
            amount = Money.Create(request.Amount, request.Currency);
        }
        catch (Exception ex)
        {
            return Result.Failure<TransactionResponse>(ex.Message, "INVALID_AMOUNT");
        }

        // Check transaction limits
        var limitResult = await _limitService.CheckLimitAsync(
            request.AccountId,
            account.UserId,
            TransactionType.Withdrawal,
            request.Amount,
            cancellationToken);

        if (!limitResult.IsAllowed)
            return Result.Failure<TransactionResponse>(
                $"{limitResult.ViolatedLimit}. Limit: {limitResult.LimitAmount:C}, Used: {limitResult.CurrentUsage:C}, Remaining: {limitResult.RemainingAmount:C}",
                "LIMIT_EXCEEDED");

        // Check for fraud
        var fraudResult = await _fraudService.CheckTransactionAsync(
            request.AccountId,
            account.UserId,
            TransactionType.Withdrawal,
            request.Amount,
            cancellationToken: cancellationToken);

        if (!fraudResult.IsAllowed)
            return Result.Failure<TransactionResponse>(
                fraudResult.BlockReason ?? "Transaction blocked by fraud detection",
                "FRAUD_DETECTED");

        // Create transaction
        var transaction = Transaction.CreateWithdrawal(
            request.AccountId,
            amount,
            request.Description,
            request.IdempotencyKey);

        try
        {
            // Perform withdrawal
            account.Withdraw(amount);
            transaction.MarkCompleted(sourceBalanceAfter: account.Balance);
        }
        catch (Exception ex)
        {
            transaction.MarkFailed(ex.Message);
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Failure<TransactionResponse>(ex.Message, "WITHDRAWAL_FAILED");
        }

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        return new TransactionResponse
        {
            TransactionId = transaction.Id,
            ReferenceNumber = transaction.ReferenceNumber,
            Type = transaction.Type,
            Status = transaction.Status,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            NewBalance = account.Balance,
            Timestamp = transaction.CreatedAt
        };
    }
}
