using System.Reflection;
using CoreBank.Application.Common.Interfaces;
using CoreBank.Domain.Common;
using CoreBank.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService) : base(options)
    {
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<TransactionLimit> TransactionLimits => Set<TransactionLimit>();
    public DbSet<ScheduledPayment> ScheduledPayments => Set<ScheduledPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Apply soft delete filter globally
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var falseConstant = System.Linq.Expressions.Expression.Constant(false);
                var comparison = System.Linq.Expressions.Expression.Equal(property, falseConstant);
                var lambda = System.Linq.Expressions.Expression.Lambda(comparison, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = _currentUserService.UserId;
                    entry.Entity.CreatedAt = _dateTimeService.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedBy = _currentUserService.UserId;
                    entry.Entity.UpdatedAt = _dateTimeService.UtcNow;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = _dateTimeService.UtcNow;
                entry.Entity.DeletedBy = _currentUserService.UserId;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
