using CoreBank.Application.Common.Models;
using MediatR;

namespace CoreBank.Application.Users.Commands.LoginUser;

public record LoginUserCommand : IRequest<Result<LoginUserResponse>>
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
}

public record LoginUserResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string Role { get; init; } = null!;
    public string AccessToken { get; init; } = null!;
    public string RefreshToken { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }
}
