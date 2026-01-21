using CoreBank.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreBank.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(string email, string verificationToken, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
        var verificationLink = $"{baseUrl}/api/v1/auth/verify-email?token={verificationToken}";

        // In production, this would send an actual email via SMTP/SendGrid/etc.
        _logger.LogInformation(
            "Email verification requested for {Email}. Verification link: {Link}",
            email, verificationLink);

        await Task.CompletedTask;
    }

    public async Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
        var resetLink = $"{baseUrl}/reset-password?token={resetToken}";

        _logger.LogInformation(
            "Password reset requested for {Email}. Reset link: {Link}",
            email, resetLink);

        await Task.CompletedTask;
    }

    public async Task SendTransactionReceiptAsync(string email, string transactionRef, decimal amount, string type, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Transaction receipt for {Email}. Reference: {Ref}, Amount: {Amount:C}, Type: {Type}",
            email, transactionRef, amount, type);

        await Task.CompletedTask;
    }

    public async Task SendAccountNotificationAsync(string email, string subject, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Account notification for {Email}. Subject: {Subject}, Message: {Message}",
            email, subject, message);

        await Task.CompletedTask;
    }
}
