using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using CoreBank.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Transactions.Commands.Deposit;

public class DepositCommandHandler : IRequestHandler<DepositCommand, Result<TransactionResponse>>
{
    private readonly IApplicationDbContext _context;

    public DepositCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<TransactionResponse>> Handle(
        DepositCommand request,
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
                    NewBalance = existingTransaction.BalanceAfterDestination,
                    Timestamp = existingTransaction.CreatedAt
                };
            }
        }

        // Validate amount
        if (request.Amount <= 0)
            return Result.Failure<TransactionResponse>("Amount must be greater than zero", "INVALID_AMOUNT");

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

        // Create transaction
        var transaction = Transaction.CreateDeposit(
            request.AccountId,
            amount,
            request.Description,
            request.IdempotencyKey);

        try
        {
            // Perform deposit
            account.Deposit(amount);
            transaction.MarkCompleted(destBalanceAfter: account.Balance);
        }
        catch (Exception ex)
        {
            transaction.MarkFailed(ex.Message);
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Failure<TransactionResponse>(ex.Message, "DEPOSIT_FAILED");
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
