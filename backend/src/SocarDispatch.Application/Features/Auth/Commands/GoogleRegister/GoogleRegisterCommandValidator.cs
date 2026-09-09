using FluentValidation;

namespace SocarDispatch.Application.Features.Auth.Commands.GoogleRegister;

public class GoogleRegisterCommandValidator : AbstractValidator<GoogleRegisterCommand>
{
    public GoogleRegisterCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID Token is required.");
    }
}
