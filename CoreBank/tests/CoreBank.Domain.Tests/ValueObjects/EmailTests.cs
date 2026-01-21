using CoreBank.Domain.Exceptions;
using CoreBank.Domain.ValueObjects;
using FluentAssertions;

namespace CoreBank.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+tag@example.com")]
    public void Create_WithValidEmail_ShouldCreateEmail(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        result.Value.Should().Be(email.ToLower());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyEmail_ShouldThrowDomainException(string? email)
    {
        // Act & Assert
        var act = () => Email.Create(email!);
        act.Should().Throw<DomainException>()
            .WithMessage("Email is required");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@domain.com")]
    [InlineData("invalid@domain")]
    public void Create_WithInvalidEmail_ShouldThrowDomainException(string email)
    {
        // Act & Assert
        var act = () => Email.Create(email);
        act.Should().Throw<DomainException>()
            .WithMessage("Invalid email format");
    }

    [Fact]
    public void Create_ShouldNormalizeToLowercase()
    {
        // Act
        var result = Email.Create("TEST@EXAMPLE.COM");

        // Assert
        result.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnValue()
    {
        // Arrange
        var email = Email.Create("test@example.com");

        // Act
        string value = email;

        // Assert
        value.Should().Be("test@example.com");
    }
}
