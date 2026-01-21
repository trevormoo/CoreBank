using CoreBank.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreBank.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AccountNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(a => a.AccountNumber)
            .IsUnique();

        builder.Property(a => a.Balance)
            .HasPrecision(18, 2);

        builder.Property(a => a.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(a => a.AccountType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.DailyWithdrawalLimit)
            .HasPrecision(18, 2);

        builder.Property(a => a.DailyWithdrawalUsed)
            .HasPrecision(18, 2);

        builder.Property(a => a.InterestRate)
            .HasPrecision(5, 4);

        // Relationships
        builder.HasOne(a => a.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.AccountType);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.IsDeleted);
    }
}
