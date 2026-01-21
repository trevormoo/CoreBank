using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreBank.Application.Statements.Queries.GenerateStatement;

public class GenerateStatementQueryHandler
    : IRequestHandler<GenerateStatementQuery, Result<GenerateStatementResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IStatementService _statementService;

    public GenerateStatementQueryHandler(
        IApplicationDbContext context,
        IStatementService statementService)
    {
        _context = context;
        _statementService = statementService;
    }

    public async Task<Result<GenerateStatementResponse>> Handle(
        GenerateStatementQuery request,
        CancellationToken cancellationToken)
    {
        // Verify account exists
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && !a.IsDeleted, cancellationToken);

        if (account is null)
            return Result.Failure<GenerateStatementResponse>("Account not found", "ACCOUNT_NOT_FOUND");

        // Check authorization
        if (request.RequestingUserId.HasValue && account.UserId != request.RequestingUserId.Value)
            return Result.Failure<GenerateStatementResponse>("Access denied", "ACCESS_DENIED");

        // Validate date range
        if (request.FromDate > request.ToDate)
            return Result.Failure<GenerateStatementResponse>(
                "From date must be before or equal to To date",
                "INVALID_DATE_RANGE");

        // Maximum 1 year range
        if ((request.ToDate - request.FromDate).TotalDays > 366)
            return Result.Failure<GenerateStatementResponse>(
                "Date range cannot exceed 1 year",
                "DATE_RANGE_TOO_LARGE");

        try
        {
            var pdfContent = await _statementService.GenerateAccountStatementAsync(
                request.AccountId,
                request.FromDate,
                request.ToDate,
                cancellationToken);

            var fileName = $"Statement_{account.AccountNumber}_{request.FromDate:yyyyMMdd}_{request.ToDate:yyyyMMdd}.pdf";

            return new GenerateStatementResponse
            {
                PdfContent = pdfContent,
                FileName = fileName
            };
        }
        catch (Exception ex)
        {
            return Result.Failure<GenerateStatementResponse>(
                $"Failed to generate statement: {ex.Message}",
                "GENERATION_FAILED");
        }
    }
}
