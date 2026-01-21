using CoreBank.Application.Common.Interfaces;
using CoreBank.Application.Transactions.Commands.Transfer;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using CoreBank.Domain.ValueObjects;
using CoreBank.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CoreBank.Application.Tests.Transactions;

public class TransferCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TransferCommandHandler _handler;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTimeService> _dateTimeServiceMock;
    private readonly Mock<ITransactionLimitService> _limitServiceMock;
    private readonly Mock<IFraudDetectionService> _fraudServiceMock;

    public TransferCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns("test-user");

        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        _limitServiceMock = new Mock<ITransactionLimitService>();
        _limitServiceMock.Setup(x => x.CheckLimitAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<TransactionType>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionLimitResult { IsAllowed = true });

        _fraudServiceMock = new Mock<IFraudDetectionService>();
        _fraudServiceMock.Setup(x => x.CheckTransactionAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<TransactionType>(),
                It.IsAny<decimal>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FraudCheckResult { IsAllowed = true, RiskLevel = FraudRiskLevel.Low });

        _context = new ApplicationDbContext(options, _currentUserServiceMock.Object, _dateTimeServiceMock.Object);
        _handler = new TransferCommandHandler(_context, _limitServiceMock.Object, _fraudServiceMock.Object);
    }

    private async Task<(User user, Account source, Account destination)> SetupAccountsAsync(
        decimal sourceBalance = 1000m,
        decimal destBalance = 500m)
    {
        var user = User.Create(
            Email.Create("transfer@example.com"),
            "hashedPassword",
            "John",
            "Doe");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var sourceAccount = Account.Create(user.Id, AccountType.Savings, "USD");
        sourceAccount.Deposit(Money.Create(sourceBalance, "USD"));

        var destAccount = Account.Create(user.Id, AccountType.Savings, "USD");
        destAccount.Deposit(Money.Create(destBalance, "USD"));

        _context.Accounts.AddRange(sourceAccount, destAccount);
        await _context.SaveChangesAsync();

        return (user, sourceAccount, destAccount);
    }

    [Fact]
    public async Task Handle_WithValidTransfer_ShouldSucceed()
    {
        // Arrange
        var (_, source, dest) = await SetupAccountsAsync(1000, 500);

        var command = new TransferCommand
        {
            SourceAccountId = source.Id,
            DestinationAccountId = dest.Id,
            Amount = 200,
            Currency = "USD",
            Description = "Test transfer"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be(TransactionStatus.Completed);
        result.Value.Amount.Should().Be(200);
        result.Value.SourceNewBalance.Should().Be(800);
        result.Value.DestinationNewBalance.Should().Be(700);
    }

    [Fact]
    public async Task Handle_WithInsufficientFunds_ShouldFail()
    {
        // Arrange
        var (_, source, dest) = await SetupAccountsAsync(100, 500);

        var command = new TransferCommand
        {
            SourceAccountId = source.Id,
            DestinationAccountId = dest.Id,
            Amount = 200, // More than balance
            Currency = "USD"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("TRANSFER_FAILED");
    }

    [Fact]
    public async Task Handle_WithNonExistentSourceAccount_ShouldFail()
    {
        // Arrange
        var (_, _, dest) = await SetupAccountsAsync();

        var command = new TransferCommand
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = dest.Id,
            Amount = 100,
            Currency = "USD"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("SOURCE_ACCOUNT_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WithNonExistentDestinationAccount_ShouldFail()
    {
        // Arrange
        var (_, source, _) = await SetupAccountsAsync();

        var command = new TransferCommand
        {
            SourceAccountId = source.Id,
            DestinationAccountId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("DESTINATION_ACCOUNT_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_WithLimitExceeded_ShouldFail()
    {
        // Arrange
        var (_, source, dest) = await SetupAccountsAsync();

        _limitServiceMock.Setup(x => x.CheckLimitAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<TransactionType>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionLimitResult
            {
                IsAllowed = false,
                ViolatedLimit = "Daily limit",
                LimitAmount = 500,
                CurrentUsage = 400,
                RemainingAmount = 100
            });

        var command = new TransferCommand
        {
            SourceAccountId = source.Id,
            DestinationAccountId = dest.Id,
            Amount = 200,
            Currency = "USD"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("LIMIT_EXCEEDED");
    }

    [Fact]
    public async Task Handle_WithFraudDetected_ShouldFail()
    {
        // Arrange
        var (_, source, dest) = await SetupAccountsAsync();

        _fraudServiceMock.Setup(x => x.CheckTransactionAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<TransactionType>(),
                It.IsAny<decimal>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FraudCheckResult
            {
                IsAllowed = false,
                RiskLevel = FraudRiskLevel.Critical,
                BlockReason = "Suspicious activity detected"
            });

        var command = new TransferCommand
        {
            SourceAccountId = source.Id,
            DestinationAccountId = dest.Id,
            Amount = 200,
            Currency = "USD"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("FRAUD_DETECTED");
    }

    [Fact]
    public async Task Handle_WithIdempotencyKey_ShouldReturnExistingTransaction()
    {
        // Arrange
        var (_, source, dest) = await SetupAccountsAsync();
        var idempotencyKey = "unique-key-123";

        var command = new TransferCommand
        {
            SourceAccountId = source.Id,
            DestinationAccountId = dest.Id,
            Amount = 100,
            Currency = "USD",
            IdempotencyKey = idempotencyKey
        };

        // First call
        var result1 = await _handler.Handle(command, CancellationToken.None);

        // Act - Second call with same idempotency key
        var result2 = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result2.Value!.TransactionId.Should().Be(result1.Value!.TransactionId);

        // Verify only one transaction was created
        var transactions = await _context.Transactions.CountAsync();
        transactions.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldCreateTransactionRecord()
    {
        // Arrange
        var (_, source, dest) = await SetupAccountsAsync();

        var command = new TransferCommand
        {
            SourceAccountId = source.Id,
            DestinationAccountId = dest.Id,
            Amount = 150,
            Currency = "USD",
            Description = "Payment for services"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var transaction = await _context.Transactions.FirstOrDefaultAsync();
        transaction.Should().NotBeNull();
        transaction!.Type.Should().Be(TransactionType.Transfer);
        transaction.Amount.Should().Be(150);
        transaction.Description.Should().Be("Payment for services");
        transaction.Status.Should().Be(TransactionStatus.Completed);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
