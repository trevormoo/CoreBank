using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Users.Commands.LoginUser;
using CoreBank.Domain.Entities;
using CoreBank.Domain.ValueObjects;
using CoreBank.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CoreBank.Application.Tests.Users;

public class LoginUserCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly LoginUserCommandHandler _handler;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTimeService> _dateTimeServiceMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;

    public LoginUserCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("system");

        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("test_access_token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("test_refresh_token");

        _context = new ApplicationDbContext(options, _currentUserServiceMock.Object, _dateTimeServiceMock.Object);
        _handler = new LoginUserCommandHandler(_context, _passwordHasherMock.Object, _tokenServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var user = User.Create(
            Email.Create("login@example.com"),
            "hashed_password",
            "John",
            "Doe");
        user.VerifyEmail(); // User must have verified email
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasherMock.Setup(x => x.VerifyPassword("correct_password", "hashed_password"))
            .Returns(true);

        var command = new LoginUserCommand
        {
            Email = "login@example.com",
            Password = "correct_password"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("test_access_token");
        result.Value.RefreshToken.Should().Be("test_refresh_token");
        result.Value.Email.Should().Be("login@example.com");
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new LoginUserCommand
        {
            Email = "nonexistent@example.com",
            Password = "password"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        var user = User.Create(
            Email.Create("wrongpass@example.com"),
            "hashed_password",
            "John",
            "Doe");
        user.VerifyEmail();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasherMock.Setup(x => x.VerifyPassword("wrong_password", "hashed_password"))
            .Returns(false);

        var command = new LoginUserCommand
        {
            Email = "wrongpass@example.com",
            Password = "wrong_password"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Handle_WithUnverifiedEmail_ShouldReturnFailure()
    {
        // Arrange
        var user = User.Create(
            Email.Create("unverified@example.com"),
            "hashed_password",
            "John",
            "Doe");
        // Don't verify email
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasherMock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var command = new LoginUserCommand
        {
            Email = "unverified@example.com",
            Password = "password"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("EMAIL_NOT_VERIFIED");
    }

    [Fact]
    public async Task Handle_WithDeactivatedAccount_ShouldReturnFailure()
    {
        // Arrange
        var user = User.Create(
            Email.Create("deactivated@example.com"),
            "hashed_password",
            "John",
            "Doe");
        user.VerifyEmail();
        user.Deactivate();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasherMock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var command = new LoginUserCommand
        {
            Email = "deactivated@example.com",
            Password = "password"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("ACCOUNT_DEACTIVATED");
    }

    [Fact]
    public async Task Handle_ShouldRecordLoginTime()
    {
        // Arrange
        var user = User.Create(
            Email.Create("record@example.com"),
            "hashed_password",
            "John",
            "Doe");
        user.VerifyEmail();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasherMock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var command = new LoginUserCommand
        {
            Email = "record@example.com",
            Password = "password"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedUser = await _context.Users.FirstAsync(u => u.Email == "record@example.com");
        updatedUser.LastLoginAt.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
