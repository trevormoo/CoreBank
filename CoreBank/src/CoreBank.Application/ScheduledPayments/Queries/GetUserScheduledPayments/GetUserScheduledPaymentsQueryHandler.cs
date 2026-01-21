using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.ScheduledPayments.Queries.GetUserScheduledPayments;

public class GetUserScheduledPaymentsQueryHandler
    : IRequestHandler<GetUserScheduledPaymentsQuery, Result<List<ScheduledPaymentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetUserScheduledPaymentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<ScheduledPaymentDto>>> Handle(
        GetUserScheduledPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.ScheduledPayments
            .Include(sp => sp.SourceAccount)
            .Include(sp => sp.DestinationAccount)
            .Where(sp => sp.SourceAccount.UserId == request.UserId && !sp.IsDeleted);

        if (request.ActiveOnly == true)
            query = query.Where(sp => sp.IsActive);

        var payments = await query
            .OrderByDescending(sp => sp.CreatedAt)
            .Select(sp => new ScheduledPaymentDto
            {
                Id = sp.Id,
                SourceAccountId = sp.SourceAccountId,
                SourceAccountNumber = sp.SourceAccount.AccountNumber,
                DestinationAccountId = sp.DestinationAccountId,
                DestinationAccountNumber = sp.DestinationAccount.AccountNumber,
                Amount = sp.Amount,
                Currency = sp.Currency,
                Description = sp.Description,
                Frequency = sp.Frequency,
                StartDate = sp.StartDate,
                EndDate = sp.EndDate,
                NextExecutionDate = sp.NextExecutionDate,
                LastExecutionDate = sp.LastExecutionDate,
                ExecutionCount = sp.ExecutionCount,
                MaxExecutions = sp.MaxExecutions,
                IsActive = sp.IsActive,
                LastError = sp.LastError,
                CreatedAt = sp.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return payments;
    }
}
