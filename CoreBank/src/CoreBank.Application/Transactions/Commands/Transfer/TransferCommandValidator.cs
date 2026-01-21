using FluentValidation;

namespace CoreBank.Application.Transactions.Commands.Transfer;

public class TransferCommandValidator : AbstractValidator<TransferCommand>
{
    public TransferCommandValidator()
    {
        RuleFor(x => x.SourceAccountId)
            .NotEmpty()
            .WithMessage("Source account ID is required");

        RuleFor(x => x.DestinationAccountId)
            .NotEmpty()
            .WithMessage("Destination account ID is required");

        RuleFor(x => x)
            .Must(x => x.SourceAccountId != x.DestinationAccountId)
            .WithMessage("Cannot transfer to the same account");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO code");
    }
}
