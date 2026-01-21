using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Users.Commands.RegisterUser;
using CoreBank.Domain.Entities;
using CoreBank.Domain.ValueObjects;
using CoreBank.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CoreBank.Application.Tests.Users;

public class RegisterUserCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RegisterUserCommandHandler _handler;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTimeService> _dateTimeServiceMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IEmailService> _emailServiceMock;

    public RegisterUserCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("system");

        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        _passwordHasherMock = new Mock<IPasswordHasher>();
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashed_password");

        _emailServiceMock = new Mock<IEmailService>();

        _context = new ApplicationDbContext(options, _currentUserServiceMock.Object, _dateTimeServiceMock.Object);
        _handler = new RegisterUserCommandHandler(_context, _passwordHasherMock.Object, _emailServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be("newuser@example.com");
        result.Value.FullName.Should().Be("John Doe");

        var savedUser = await _context.Users.FirstOrDefaultAsync();
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be("newuser@example.com");
        savedUser.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ShouldReturnFailure()
    {
        // Arrange
        var existingUser = User.Create(
            Email.Create("existing@example.com"),
            "hashedPassword",
            "Existing",
            "User");
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var command = new RegisterUserCommand
        {
            Email = "existing@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
        result.ErrorCode.Should().Be("EMAIL_EXISTS");
    }

    [Fact]
    public async Task Handle_ShouldHashPassword()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "user@example.com",
            Password = "MySecretPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasherMock.Verify(x => x.HashPassword("MySecretPassword123!"), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSendVerificationEmail()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "verify@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(
            x => x.SendEmailVerificationAsync(
                "verify@example.com",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithPhoneNumber_ShouldCreateUserWithPhone()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "phone@example.com",
            Password = "Password123!",
            FirstName = "Phone",
            LastName = "User",
            PhoneNumber = "+1234567890"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "phone@example.com");
        user.Should().NotBeNull();
        user!.PhoneNumber.Should().Be("+1234567890");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
