using CoreBank.Application.Accounts.Commands.CreateAccount;
using CoreBank.Application.Common.Interfaces;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using CoreBank.Domain.ValueObjects;
using CoreBank.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CoreBank.Application.Tests.Accounts;

public class CreateAccountCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CreateAccountCommandHandler _handler;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTimeService> _dateTimeServiceMock;

    public CreateAccountCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user");

        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        _context = new ApplicationDbContext(options, _currentUserServiceMock.Object, _dateTimeServiceMock.Object);
        _handler = new CreateAccountCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WithValidUser_ShouldCreateAccount()
    {
        // Arrange
        var user = User.Create(
            Email.Create("test@example.com"),
            "hashedPassword",
            "John",
            "Doe");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new CreateAccountCommand
        {
            UserId = user.Id,
            AccountType = AccountType.Savings,
            Currency = "USD"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccountType.Should().Be(AccountType.Savings);
        result.Value.Currency.Should().Be("USD");
        result.Value.AccountNumber.Should().HaveLength(10);

        var savedAccount = await _context.Accounts.FirstOrDefaultAsync();
        savedAccount.Should().NotBeNull();
        savedAccount!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateAccountCommand
        {
            UserId = Guid.NewGuid(),
            AccountType = AccountType.Savings,
            Currency = "USD"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User not found");
        result.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ShouldReturnFailure()
    {
        // Arrange
        var user = User.Create(
            Email.Create("inactive@example.com"),
            "hashedPassword",
            "Jane",
            "Doe");

        user.Deactivate();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new CreateAccountCommand
        {
            UserId = user.Id,
            AccountType = AccountType.Savings,
            Currency = "USD"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User account is not active");
        result.ErrorCode.Should().Be("USER_INACTIVE");
    }

    [Fact]
    public async Task Handle_FixedDepositWithoutKyc_ShouldReturnFailure()
    {
        // Arrange
        var user = User.Create(
            Email.Create("nokyc@example.com"),
            "hashedPassword",
            "Bob",
            "Smith");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new CreateAccountCommand
        {
            UserId = user.Id,
            AccountType = AccountType.FixedDeposit,
            Currency = "USD"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("KYC");
        result.ErrorCode.Should().Be("KYC_REQUIRED");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
