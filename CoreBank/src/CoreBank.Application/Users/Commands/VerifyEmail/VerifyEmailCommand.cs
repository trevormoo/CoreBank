using CoreBank.Application.Common.Models;
using MediatR;

namespace CoreBank.Application.Users.Commands.VerifyEmail;

public record VerifyEmailCommand : IRequest<Result<VerifyEmailResponse>>
{
    public string Email { get; init; } = null!;
    public string Token { get; init; } = null!;
}

public record VerifyEmailResponse
{
    public string Message { get; init; } = null!;
}
