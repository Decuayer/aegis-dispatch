using FluentValidation;

namespace SocarDispatch.Application.Features.Auth.Commands.GoogleLogin;

public class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator()
    {
        RuleFor(v => v.IdToken)
            .NotEmpty().WithMessage("The Google ID Token is mandatory.");
    }
}
