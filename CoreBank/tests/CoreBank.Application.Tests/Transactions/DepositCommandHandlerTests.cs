using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Transactions.Commands.Deposit;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using CoreBank.Domain.ValueObjects;
using CoreBank.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CoreBank.Application.Tests.Transactions;

public class DepositCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DepositCommandHandler _handler;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTimeService> _dateTimeServiceMock;

    public DepositCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user");

        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        _context = new ApplicationDbContext(options, _currentUserServiceMock.Object, _dateTimeServiceMock.Object);
        _handler = new DepositCommandHandler(_context);
    }

    private async Task<Account> SetupAccountAsync(decimal initialBalance = 0m)
    {
        var user = User.Create(
            Email.Create("deposit@example.com"),
            "hashedPassword",
            "John",
            "Doe");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var account = Account.Create(user.Id, AccountType.Savings, "USD");
        if (initialBalance > 0)
        {
            account.Deposit(Money.Create(initialBalance, "USD"));
        }

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return account;
    }

    [Fact]
    public async Task Handle_WithValidDeposit_ShouldSucceed()
    {
        // Arrange
        var account = await SetupAccountAsync(500);

        var command = new DepositCommand
        {
            AccountId = account.Id,
            Amount = 250,
            Currency = "USD",
            Description = "Salary deposit"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Type.Should().Be(TransactionType.Deposit);
        result.Value.Amount.Should().Be(250);
        result.Value.NewBalance.Should().Be(750);
        result.Value.Status.Should().Be(TransactionStatus.Completed);
    }

    [Fact]
    public async Task Handle_WithNonExistentAccount_ShouldFail()
    {
        // Arrange
        var command = new DepositCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("ACCOUNT_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WithZeroAmount_ShouldFail()
    {
        // Arrange
        var account = await SetupAccountAsync();

        var command = new DepositCommand
        {
            AccountId = account.Id,
            Amount = 0,
            Currency = "USD"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("INVALID_AMOUNT");
    }

    [Fact]
    public async Task Handle_WithNegativeAmount_ShouldFail()
    {
        // Arrange
        var account = await SetupAccountAsync();

        var command = new DepositCommand
        {
            AccountId = account.Id,
            Amount = -100,
            Currency = "USD"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("INVALID_AMOUNT");
    }

    [Fact]
    public async Task Handle_WithIdempotencyKey_ShouldReturnExistingTransaction()
    {
        // Arrange
        var account = await SetupAccountAsync();
        var idempotencyKey = "deposit-key-456";

        var command = new DepositCommand
        {
            AccountId = account.Id,
            Amount = 500,
            Currency = "USD",
            IdempotencyKey = idempotencyKey
        };

        // First deposit
        var result1 = await _handler.Handle(command, CancellationToken.None);

        // Act - Second deposit with same key
        var result2 = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result2.Value!.TransactionId.Should().Be(result1.Value!.TransactionId);

        // Balance should only increase once
        var updatedAccount = await _context.Accounts.FirstAsync(a => a.Id == account.Id);
        updatedAccount.Balance.Should().Be(500);
    }

    [Fact]
    public async Task Handle_ShouldCreateTransactionRecord()
    {
        // Arrange
        var account = await SetupAccountAsync();

        var command = new DepositCommand
        {
            AccountId = account.Id,
            Amount = 1000,
            Currency = "USD",
            Description = "Initial deposit"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var transaction = await _context.Transactions.FirstOrDefaultAsync();
        transaction.Should().NotBeNull();
        transaction!.Type.Should().Be(TransactionType.Deposit);
        transaction.Amount.Should().Be(1000);
        transaction.DestinationAccountId.Should().Be(account.Id);
        transaction.SourceAccountId.Should().BeNull();
        transaction.ReferenceNumber.Should().StartWith("TXN");
    }

    [Fact]
    public async Task Handle_MultipleDeposits_ShouldAccumulateBalance()
    {
        // Arrange
        var account = await SetupAccountAsync(1000);

        // Act
        await _handler.Handle(new DepositCommand { AccountId = account.Id, Amount = 100, Currency = "USD" }, CancellationToken.None);
        await _handler.Handle(new DepositCommand { AccountId = account.Id, Amount = 200, Currency = "USD" }, CancellationToken.None);
        await _handler.Handle(new DepositCommand { AccountId = account.Id, Amount = 300, Currency = "USD" }, CancellationToken.None);

        // Assert
        var updatedAccount = await _context.Accounts.FirstAsync(a => a.Id == account.Id);
        updatedAccount.Balance.Should().Be(1600); // 1000 + 100 + 200 + 300
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
