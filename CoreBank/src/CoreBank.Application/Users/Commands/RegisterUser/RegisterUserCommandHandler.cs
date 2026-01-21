using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using CoreBank.Domain.Entities;
using CoreBank.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    public RegisterUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IEmailService emailService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
    }

    public async Task<Result<RegisterUserResponse>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted, cancellationToken);

        if (existingUser is not null)
            return Result.Failure<RegisterUserResponse>("An account with this email already exists", "EMAIL_EXISTS");

        // Create email value object (validates format)
        Email email;
        try
        {
            email = Email.Create(request.Email);
        }
        catch (Exception ex)
        {
            return Result.Failure<RegisterUserResponse>(ex.Message, "INVALID_EMAIL");
        }

        // Create phone number value object if provided
        PhoneNumber? phoneNumber = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            try
            {
                phoneNumber = PhoneNumber.Create(request.PhoneNumber);
            }
            catch (Exception ex)
            {
                return Result.Failure<RegisterUserResponse>(ex.Message, "INVALID_PHONE");
            }
        }

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create user
        var user = User.Create(
            email,
            passwordHash,
            request.FirstName,
            request.LastName,
            phoneNumber,
            request.DateOfBirth);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // Send verification email
        await _emailService.SendEmailVerificationAsync(
            user.Email,
            user.EmailVerificationToken!,
            cancellationToken);

        return new RegisterUserResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Message = "Registration successful. Please check your email to verify your account."
        };
    }
}
