using CoreBank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreBank.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ReferenceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(t => t.ReferenceNumber)
            .IsUnique();

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Property(t => t.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.FailureReason)
            .HasMaxLength(1000);

        builder.Property(t => t.IdempotencyKey)
            .HasMaxLength(64);

        builder.HasIndex(t => t.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");

        builder.Property(t => t.BalanceAfterSource)
            .HasPrecision(18, 2);

        builder.Property(t => t.BalanceAfterDestination)
            .HasPrecision(18, 2);

        // Relationships
        builder.HasOne(t => t.SourceAccount)
            .WithMany(a => a.SourceTransactions)
            .HasForeignKey(t => t.SourceAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.DestinationAccount)
            .WithMany(a => a.DestinationTransactions)
            .HasForeignKey(t => t.DestinationAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.OriginalTransaction)
            .WithOne(t => t.ReversalTransaction)
            .HasForeignKey<Transaction>(t => t.OriginalTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(t => t.SourceAccountId);
        builder.HasIndex(t => t.DestinationAccountId);
        builder.HasIndex(t => t.Type);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => new { t.SourceAccountId, t.CreatedAt });
        builder.HasIndex(t => new { t.DestinationAccountId, t.CreatedAt });
    }
}
