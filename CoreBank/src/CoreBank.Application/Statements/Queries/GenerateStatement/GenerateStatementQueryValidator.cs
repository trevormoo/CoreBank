using FluentValidation;

namespace CoreBank.Application.Statements.Queries.GenerateStatement;

public class GenerateStatementQueryValidator : AbstractValidator<GenerateStatementQuery>
{
    public GenerateStatementQueryValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.FromDate)
            .NotEmpty()
            .WithMessage("From date is required")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("From date cannot be in the future");

        RuleFor(x => x.ToDate)
            .NotEmpty()
            .WithMessage("To date is required")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("To date cannot be in the future")
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("To date must be after or equal to From date");
    }
}
