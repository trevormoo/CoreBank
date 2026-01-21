using System.Security.Claims;
using CoreBank.Application.Common.Models;
using CoreBank.Application.Transactions.Commands.Deposit;
using CoreBank.Application.Transactions.Commands.Transfer;
using CoreBank.Application.Transactions.Commands.Withdraw;
using CoreBank.Application.Transactions.Queries.GetTransactionHistory;
using CoreBank.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreBank.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Deposit money into an account
    /// </summary>
    [HttpPost("deposit")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Deposit(
        [FromBody] DepositRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new DepositCommand
        {
            AccountId = request.AccountId,
            Amount = request.Amount,
            Currency = request.Currency ?? "USD",
            Description = request.Description,
            IdempotencyKey = idempotencyKey
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { message = error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Withdraw money from an account
    /// </summary>
    [HttpPost("withdraw")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Withdraw(
        [FromBody] WithdrawRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new WithdrawCommand
        {
            AccountId = request.AccountId,
            Amount = request.Amount,
            Currency = request.Currency ?? "USD",
            Description = request.Description,
            IdempotencyKey = idempotencyKey
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { message = error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Transfer money between accounts
    /// </summary>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Transfer(
        [FromBody] TransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new TransferCommand
        {
            SourceAccountId = request.SourceAccountId,
            DestinationAccountId = request.DestinationAccountId,
            Amount = request.Amount,
            Currency = request.Currency ?? "USD",
            Description = request.Description,
            IdempotencyKey = idempotencyKey
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { message = error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Get transaction history for an account
    /// </summary>
    [HttpGet("history/{accountId:guid}")]
    [ProducesResponseType(typeof(PaginatedList<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionHistory(
        Guid accountId,
        [FromQuery] TransactionType? type,
        [FromQuery] TransactionStatus? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var query = new GetTransactionHistoryQuery
        {
            AccountId = accountId,
            RequestingUserId = IsAdmin() ? null : userId,
            Type = type,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => result.ErrorCode switch
            {
                "ACCESS_DENIED" => Forbid(),
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

public record DepositRequest
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public string? Description { get; init; }
}

public record WithdrawRequest
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public string? Description { get; init; }
}

public record TransferRequest
{
    public Guid SourceAccountId { get; init; }
    public Guid DestinationAccountId { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public string? Description { get; init; }
}
