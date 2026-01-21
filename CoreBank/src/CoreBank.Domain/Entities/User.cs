using CoreBank.Domain.Common;
using CoreBank.Domain.Enums;
using CoreBank.Domain.ValueObjects;

namespace CoreBank.Domain.Entities;

public class User : AuditableEntity, ISoftDelete
{
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public UserRole Role { get; private set; }
    public KycStatus KycStatus { get; private set; }
    public bool EmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiry { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiry { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public virtual ICollection<Account> Accounts { get; private set; } = new List<Account>();
    public virtual ICollection<KycDocument> KycDocuments { get; private set; } = new List<KycDocument>();

    private User() { }

    public static User Create(
        Email email,
        string passwordHash,
        string firstName,
        string lastName,
        PhoneNumber? phoneNumber = null,
        DateTime? dateOfBirth = null)
    {
        var user = new User
        {
            Email = email.Value,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber?.Value,
            DateOfBirth = dateOfBirth,
            Role = UserRole.Customer,
            KycStatus = KycStatus.Pending,
            EmailVerified = false,
            IsActive = true,
            EmailVerificationToken = Guid.NewGuid().ToString("N"),
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
        };

        return user;
    }

    public string FullName => $"{FirstName} {LastName}";

    public void VerifyEmail()
    {
        EmailVerified = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiry = null;
    }

    public void UpdateProfile(string firstName, string lastName, PhoneNumber? phoneNumber, DateTime? dateOfBirth)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber?.Value;
        DateOfBirth = dateOfBirth;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
    }

    public void GeneratePasswordResetToken()
    {
        PasswordResetToken = Guid.NewGuid().ToString("N");
        PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
    }

    public void UpdateKycStatus(KycStatus status)
    {
        KycStatus = status;
    }

    public void SetRole(UserRole role)
    {
        Role = role;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
}
