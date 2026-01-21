using FluentValidation;

namespace CoreBank.Application.Accounts.Commands.CreateAccount;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    private static readonly string[] SupportedCurrencies = { "USD", "EUR", "GBP", "NGN" };

    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.AccountType)
            .IsInEnum()
            .WithMessage("Invalid account type");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO code")
            .Must(c => SupportedCurrencies.Contains(c.ToUpperInvariant()))
            .WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}");
    }
}
