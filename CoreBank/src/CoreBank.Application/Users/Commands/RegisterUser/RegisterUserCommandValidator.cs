using FluentValidation;

namespace CoreBank.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(254)
            .WithMessage("Email is too long");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters")
            .MaximumLength(128)
            .WithMessage("Password is too long")
            .Matches("[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]")
            .WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]")
            .WithMessage("Password must contain at least one digit")
            .Matches("[^a-zA-Z0-9]")
            .WithMessage("Password must contain at least one special character");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MaximumLength(100)
            .WithMessage("First name is too long")
            .Matches("^[a-zA-Z\\s'-]+$")
            .WithMessage("First name contains invalid characters");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MaximumLength(100)
            .WithMessage("Last name is too long")
            .Matches("^[a-zA-Z\\s'-]+$")
            .WithMessage("Last name contains invalid characters");

        RuleFor(x => x.PhoneNumber)
            .Matches("^\\+?[1-9]\\d{1,14}$")
            .WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.UtcNow.AddYears(-18))
            .WithMessage("You must be at least 18 years old")
            .GreaterThan(DateTime.UtcNow.AddYears(-120))
            .WithMessage("Invalid date of birth")
            .When(x => x.DateOfBirth.HasValue);
    }
}
