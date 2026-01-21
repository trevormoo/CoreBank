using CoreBank.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Account> Accounts { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<KycDocument> KycDocuments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<TransactionLimit> TransactionLimits { get; }
    DbSet<ScheduledPayment> ScheduledPayments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
