using CoreBank.Application.Common.Models;
using MediatR;

namespace CoreBank.Application.Statements.Queries.GenerateStatement;

public record GenerateStatementQuery : IRequest<Result<GenerateStatementResponse>>
{
    public Guid AccountId { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public Guid? RequestingUserId { get; init; }
}

public record GenerateStatementResponse
{
    public byte[] PdfContent { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public string ContentType { get; init; } = "application/pdf";
}
