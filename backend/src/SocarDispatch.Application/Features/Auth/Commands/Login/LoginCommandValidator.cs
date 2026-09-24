using FluentValidation;

namespace SocarDispatch.Application.Features.Auth.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("The e-mail address is required.")
            .EmailAddress().WithMessage("Please enter a valid e-mail address.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("A password is required.");
    }
}
