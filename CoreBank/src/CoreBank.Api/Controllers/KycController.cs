using System.Security.Claims;
using CoreBank.Application.Common.Models;
using CoreBank.Application.Kyc.Commands.ReviewKycDocument;
using CoreBank.Application.Kyc.Commands.SubmitKycDocument;
using CoreBank.Application.Kyc.Queries.GetPendingKycDocuments;
using CoreBank.Application.Kyc.Queries.GetUserKycDocuments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreBank.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class KycController : ControllerBase
{
    private readonly IMediator _mediator;

    public KycController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Submit a KYC document for verification
    /// </summary>
    [HttpPost("documents")]
    [ProducesResponseType(typeof(SubmitKycDocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitDocument(
        [FromBody] SubmitKycDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var command = new SubmitKycDocumentCommand
        {
            UserId = userId,
            DocumentType = request.DocumentType,
            DocumentNumber = request.DocumentNumber,
            DocumentUrl = request.DocumentUrl,
            ExpiryDate = request.ExpiryDate
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            success => CreatedAtAction(nameof(GetMyDocuments), success),
            error => BadRequest(new { message = error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Get current user's KYC documents
    /// </summary>
    [HttpGet("documents")]
    [ProducesResponseType(typeof(List<KycDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyDocuments(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var query = new GetUserKycDocumentsQuery
        {
            UserId = userId,
            RequestingUserId = userId
        };

        var result = await _mediator.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { message = error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Get KYC documents for a specific user (Admin only)
    /// </summary>
    [HttpGet("documents/user/{userId:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(List<KycDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserDocuments(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var query = new GetUserKycDocumentsQuery
        {
            UserId = userId,
            RequestingUserId = null // Admin bypass
        };

        var result = await _mediator.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { message = error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Get all pending KYC documents (Admin only)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(PaginatedList<PendingKycDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingDocuments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPendingKycDocumentsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { message = error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Approve or reject a KYC document (Admin only)
    /// </summary>
    [HttpPost("documents/{documentId:guid}/review")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(ReviewKycDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewDocument(
        Guid documentId,
        [FromBody] ReviewKycDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var reviewerId = GetCurrentUserId().ToString();

        var command = new ReviewKycDocumentCommand
        {
            DocumentId = documentId,
            ReviewerId = reviewerId,
            Approve = request.Approve,
            RejectionReason = request.RejectionReason
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => result.ErrorCode switch
            {
                "DOCUMENT_NOT_FOUND" => NotFound(new { message = error, code = result.ErrorCode }),
                _ => BadRequest(new { message = error, code = result.ErrorCode })
            });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

public record SubmitKycDocumentRequest
{
    public string DocumentType { get; init; } = null!;
    public string DocumentNumber { get; init; } = null!;
    public string? DocumentUrl { get; init; }
    public DateTime? ExpiryDate { get; init; }
}

public record ReviewKycDocumentRequest
{
    public bool Approve { get; init; }
    public string? RejectionReason { get; init; }
}
