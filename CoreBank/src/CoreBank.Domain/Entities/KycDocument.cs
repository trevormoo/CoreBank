using CoreBank.Domain.Common;
using CoreBank.Domain.Enums;

namespace CoreBank.Domain.Entities;

public class KycDocument : AuditableEntity, ISoftDelete
{
    public Guid UserId { get; private set; }
    public string DocumentType { get; private set; } = null!;  // Passport, Driver's License, National ID
    public string DocumentNumber { get; private set; } = null!;
    public string? DocumentUrl { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public KycStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewedBy { get; private set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation
    public virtual User User { get; private set; } = null!;

    private KycDocument() { }

    public static KycDocument Create(
        Guid userId,
        string documentType,
        string documentNumber,
        string? documentUrl = null,
        DateTime? expiryDate = null)
    {
        return new KycDocument
        {
            UserId = userId,
            DocumentType = documentType,
            DocumentNumber = documentNumber,
            DocumentUrl = documentUrl,
            ExpiryDate = expiryDate,
            Status = KycStatus.Pending
        };
    }

    public void Approve(string reviewedBy)
    {
        Status = KycStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewedBy;
        RejectionReason = null;
    }

    public void Reject(string reviewedBy, string reason)
    {
        Status = KycStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewedBy;
        RejectionReason = reason;
    }
}
