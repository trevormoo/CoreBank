using FluentValidation;

namespace CoreBank.Application.ScheduledPayments.Commands.CreateScheduledPayment;

public class CreateScheduledPaymentCommandValidator : AbstractValidator<CreateScheduledPaymentCommand>
{
    private static readonly string[] ValidFrequencies = { "Daily", "Weekly", "Monthly" };

    public CreateScheduledPaymentCommandValidator()
    {
        RuleFor(x => x.SourceAccountId)
            .NotEmpty()
            .WithMessage("Source account ID is required");

        RuleFor(x => x.DestinationAccountId)
            .NotEmpty()
            .WithMessage("Destination account ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be a 3-letter code");

        RuleFor(x => x.Frequency)
            .NotEmpty()
            .WithMessage("Frequency is required")
            .Must(f => ValidFrequencies.Contains(f))
            .WithMessage($"Frequency must be one of: {string.Join(", ", ValidFrequencies)}");

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Start date cannot be in the past");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be after start date");

        RuleFor(x => x.MaxExecutions)
            .GreaterThan(0)
            .When(x => x.MaxExecutions.HasValue)
            .WithMessage("Max executions must be greater than zero");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID is required");
    }
}
