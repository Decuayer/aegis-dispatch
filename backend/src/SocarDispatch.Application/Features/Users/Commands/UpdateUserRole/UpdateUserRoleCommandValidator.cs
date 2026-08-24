using FluentValidation;

namespace SocarDispatch.Application.Features.Users.Commands.UpdateUserRole;

public class UpdateUserRoleCommandValidator : AbstractValidator<UpdateUserRoleCommand>
{
    public UpdateUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Target user ID is required.");

        RuleFor(x => x.OperatorId)
            .NotEmpty().WithMessage("Operator ID is required.");

        RuleFor(x => x.RoleType)
            .IsInEnum().WithMessage("Invalid role type.");
    }
}
