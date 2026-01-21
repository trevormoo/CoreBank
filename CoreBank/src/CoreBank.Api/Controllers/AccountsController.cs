using System.Security.Claims;
using CoreBank.Application.Accounts.Commands.CreateAccount;
using CoreBank.Application.Accounts.Queries.GetAccountById;
using CoreBank.Application.Accounts.Queries.GetUserAccounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreBank.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new bank account
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var command = new CreateAccountCommand
        {
            UserId = userId,
            AccountType = request.AccountType,
            Currency = request.Currency ?? "USD"
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            success => CreatedAtAction(nameof(GetAccount), new { id = success.AccountId }, success),
            error => BadRequest(new { message = error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Get account by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccount(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var query = new GetAccountByIdQuery
        {
            AccountId = id,
            RequestingUserId = IsAdmin() ? null : userId
        };

        var result = await _mediator.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => result.ErrorCode == "ACCESS_DENIED" ? Forbid() : NotFound(new { message = error }));
    }

    /// <summary>
    /// Get all accounts for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyAccounts(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var query = new GetUserAccountsQuery { UserId = userId };
        var result = await _mediator.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { message = error }));
    }

    /// <summary>
    /// Get accounts for a specific user (Admin only)
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(List<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserAccounts(Guid userId, CancellationToken cancellationToken)
    {
        var query = new GetUserAccountsQuery { UserId = userId };
        var result = await _mediator.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { message = error }));
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

public record CreateAccountRequest
{
    public Domain.Enums.AccountType AccountType { get; init; }
    public string? Currency { get; init; }
}
