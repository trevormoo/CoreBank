using CoreBank.Application.Common.Models;
using MediatR;

namespace CoreBank.Application.Users.Commands.RegisterUser;

public record RegisterUserCommand : IRequest<Result<RegisterUserResponse>>
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? PhoneNumber { get; init; }
    public DateTime? DateOfBirth { get; init; }
}

public record RegisterUserResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string Message { get; init; } = null!;
}
