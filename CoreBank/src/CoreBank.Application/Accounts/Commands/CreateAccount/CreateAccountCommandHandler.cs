using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Accounts.Commands.CreateAccount;

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Result<CreateAccountResponse>>
{
    private readonly IApplicationDbContext _context;

    public CreateAccountCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CreateAccountResponse>> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        // Verify user exists
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

        if (user is null)
            return Result.Failure<CreateAccountResponse>("User not found", "USER_NOT_FOUND");

        // Check if user is active
        if (!user.IsActive)
            return Result.Failure<CreateAccountResponse>("User account is not active", "USER_INACTIVE");

        // Check KYC status for certain account types
        if (request.AccountType == AccountType.FixedDeposit && user.KycStatus != KycStatus.Approved)
            return Result.Failure<CreateAccountResponse>(
                "KYC approval is required to open a Fixed Deposit account",
                "KYC_REQUIRED");

        // Create the account
        var account = Account.Create(
            request.UserId,
            request.AccountType,
            request.Currency);

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateAccountResponse
        {
            AccountId = account.Id,
            AccountNumber = account.AccountNumber,
            AccountType = account.AccountType,
            Currency = account.Currency,
            Balance = account.Balance,
            DailyWithdrawalLimit = account.DailyWithdrawalLimit,
            InterestRate = account.InterestRate
        };
    }
}
