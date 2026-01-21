using FluentValidation;

namespace CoreBank.Application.Kyc.Commands.ReviewKycDocument;

public class ReviewKycDocumentCommandValidator : AbstractValidator<ReviewKycDocumentCommand>
{
    public ReviewKycDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId)
            .NotEmpty()
            .WithMessage("Document ID is required");

        RuleFor(x => x.ReviewerId)
            .NotEmpty()
            .WithMessage("Reviewer ID is required");

        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .When(x => !x.Approve)
            .WithMessage("Rejection reason is required when rejecting a document")
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.RejectionReason))
            .WithMessage("Rejection reason is too long");
    }
}
