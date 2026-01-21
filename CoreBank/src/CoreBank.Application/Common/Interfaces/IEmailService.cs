namespace CoreBank.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string email, string verificationToken, CancellationToken cancellationToken = default);
    Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken = default);
    Task SendTransactionReceiptAsync(string email, string transactionRef, decimal amount, string type, CancellationToken cancellationToken = default);
    Task SendAccountNotificationAsync(string email, string subject, string message, CancellationToken cancellationToken = default);
}
