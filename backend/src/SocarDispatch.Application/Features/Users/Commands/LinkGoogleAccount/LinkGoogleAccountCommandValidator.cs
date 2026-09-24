using FluentValidation;

namespace SocarDispatch.Application.Features.Users.Commands.LinkGoogleAccount;

public class LinkGoogleAccountCommandValidator : AbstractValidator<LinkGoogleAccountCommand>
{
    public LinkGoogleAccountCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID Token is required.");
    }
}
