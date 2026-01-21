using FluentValidation;

namespace CoreBank.Application.Kyc.Commands.SubmitKycDocument;

public class SubmitKycDocumentCommandValidator : AbstractValidator<SubmitKycDocumentCommand>
{
    private static readonly string[] ValidDocumentTypes =
    {
        "Passport",
        "Driver's License",
        "National ID",
        "Voter's Card",
        "Residence Permit"
    };

    public SubmitKycDocumentCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.DocumentType)
            .NotEmpty()
            .WithMessage("Document type is required")
            .Must(type => ValidDocumentTypes.Contains(type))
            .WithMessage($"Document type must be one of: {string.Join(", ", ValidDocumentTypes)}");

        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .WithMessage("Document number is required")
            .MaximumLength(50)
            .WithMessage("Document number is too long");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ExpiryDate.HasValue)
            .WithMessage("Document has expired or expiry date is invalid");
    }
}
