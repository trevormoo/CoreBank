using System.Security.Claims;
using CoreBank.Application.Statements.Queries.GenerateStatement;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreBank.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class StatementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StatementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Generate account statement PDF
    /// </summary>
    [HttpGet("account/{accountId:guid}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateStatement(
        Guid accountId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var query = new GenerateStatementQuery
        {
            AccountId = accountId,
            FromDate = fromDate,
            ToDate = toDate,
            RequestingUserId = IsAdmin() ? null : userId
        };

        var result = await _mediator.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            success => File(success.PdfContent, success.ContentType, success.FileName),
            error => result.ErrorCode switch
            {
                "ACCOUNT_NOT_FOUND" => NotFound(new { message = error, code = result.ErrorCode }),
                "ACCESS_DENIED" => Forbid(),
                _ => BadRequest(new { message = error, code = result.ErrorCode })
            });
    }

    /// <summary>
    /// Generate statement for any user's account (Admin only)
    /// </summary>
    [HttpGet("admin/account/{accountId:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateStatementAdmin(
        Guid accountId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        var query = new GenerateStatementQuery
        {
            AccountId = accountId,
            FromDate = fromDate,
            ToDate = toDate,
            RequestingUserId = null // Admin bypass
        };

        var result = await _mediator.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            success => File(success.PdfContent, success.ContentType, success.FileName),
            error => result.ErrorCode switch
            {
                "ACCOUNT_NOT_FOUND" => NotFound(new { message = error, code = result.ErrorCode }),
                _ => BadRequest(new { message = error, code = result.ErrorCode })
            });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private bool IsAdmin()
    {
        return User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
    }
}
