using CoreBank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreBank.Infrastructure.Persistence.Configurations;

public class KycDocumentConfiguration : IEntityTypeConfiguration<KycDocument>
{
    public void Configure(EntityTypeBuilder<KycDocument> builder)
    {
        builder.ToTable("KycDocuments");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.DocumentType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(k => k.DocumentNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(k => k.DocumentUrl)
            .HasMaxLength(500);

        builder.Property(k => k.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(k => k.RejectionReason)
            .HasMaxLength(500);

        builder.Property(k => k.ReviewedBy)
            .HasMaxLength(100);

        // Relationships
        builder.HasOne(k => k.User)
            .WithMany(u => u.KycDocuments)
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(k => k.UserId);
        builder.HasIndex(k => k.Status);
        builder.HasIndex(k => k.IsDeleted);
    }
}
