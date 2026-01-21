using System.Security.Claims;
using CoreBank.Application.ScheduledPayments.Commands.CancelScheduledPayment;
using CoreBank.Application.ScheduledPayments.Commands.CreateScheduledPayment;
using CoreBank.Application.ScheduledPayments.Queries.GetUserScheduledPayments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreBank.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ScheduledPaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScheduledPaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new scheduled payment
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateScheduledPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateScheduledPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var command = new CreateScheduledPaymentCommand
        {
            SourceAccountId = request.SourceAccountId,
            DestinationAccountId = request.DestinationAccountId,
            Amount = request.Amount,
            Currency = request.Currency ?? "USD",
            Frequency = request.Frequency,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MaxExecutions = request.MaxExecutions,
            Description = request.Description,
            RequestingUserId = userId
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            success => CreatedAtAction(nameof(GetMyScheduledPayments), success),
            error => BadRequest(new { message = error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Get current user's scheduled payments
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ScheduledPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyScheduledPayments(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var query = new GetUserScheduledPaymentsQuery
        {
            UserId = userId,
            ActiveOnly = activeOnly
        };

        var result = await _mediator.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { message = error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Cancel a scheduled payment
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(CancelScheduledPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var command = new CancelScheduledPaymentCommand
        {
            ScheduledPaymentId = id,
            RequestingUserId = userId
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => result.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(new { message = error, code = result.ErrorCode }),
                "ACCESS_DENIED" => Forbid(),
                _ => BadRequest(new { message = error, code = result.ErrorCode })
            });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

public record CreateScheduledPaymentRequest
{
    public Guid SourceAccountId { get; init; }
    public Guid DestinationAccountId { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public string Frequency { get; init; } = null!;
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int? MaxExecutions { get; init; }
    public string? Description { get; init; }
}
