using FluentValidation;

namespace SocarDispatch.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(v => v.FirstName)
            .NotEmpty().WithMessage("The namespace is required.")
            .MaximumLength(50).WithMessage("The name can be a maximum of 50 characters long.");

        RuleFor(v => v.LastName)
            .NotEmpty().WithMessage("The surname field is required.")
            .MaximumLength(50).WithMessage("The surname can be a maximum of 50 characters long.");

        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("The e-mail address is required.")
            .EmailAddress().WithMessage("Please enter a valid e-mail address.")
            .MaximumLength(255);

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("The password must be at least 6 characters long.");

        RuleFor(v => v.Phone)
            .NotEmpty().WithMessage("A phone number is required.")
            .MaximumLength(20);

        RuleFor(v => v.Department)
            .NotEmpty().WithMessage("Department information is mandatory.")
            .MaximumLength(100);

        RuleFor(v => v.RoleType)
            .IsInEnum().WithMessage("Invalid role type.");
    }
}
