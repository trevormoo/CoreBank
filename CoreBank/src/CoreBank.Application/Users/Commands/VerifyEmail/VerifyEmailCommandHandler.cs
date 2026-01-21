using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Users.Commands.VerifyEmail;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<VerifyEmailResponse>>
{
    private readonly IApplicationDbContext _context;

    public VerifyEmailCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<VerifyEmailResponse>> Handle(
        VerifyEmailCommand request,
        CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted, cancellationToken);

        if (user is null)
            return Result.Failure<VerifyEmailResponse>("User not found", "USER_NOT_FOUND");

        // Check if already verified
        if (user.EmailVerified)
            return Result.Failure<VerifyEmailResponse>("Email is already verified", "ALREADY_VERIFIED");

        // Verify token
        if (user.EmailVerificationToken != request.Token)
            return Result.Failure<VerifyEmailResponse>("Invalid verification token", "INVALID_TOKEN");

        // Check if token is expired
        if (user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            return Result.Failure<VerifyEmailResponse>("Verification token has expired", "TOKEN_EXPIRED");

        // Verify the email
        user.VerifyEmail();
        await _context.SaveChangesAsync(cancellationToken);

        return new VerifyEmailResponse
        {
            Message = "Email verified successfully. You can now log in to your account."
        };
    }
}
