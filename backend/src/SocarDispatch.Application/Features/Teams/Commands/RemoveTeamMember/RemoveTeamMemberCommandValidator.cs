using FluentValidation;

namespace SocarDispatch.Application.Features.Teams.Commands.RemoveTeamMember;

public class RemoveTeamMemberCommandValidator : AbstractValidator<RemoveTeamMemberCommand>
{
    public RemoveTeamMemberCommandValidator()
    {
        RuleFor(v => v.TeamId)
            .NotEmpty().WithMessage("TeamId is required.");

        RuleFor(v => v.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(v => v.RequesterId)
            .NotEmpty().WithMessage("RequesterId is required.");
    }
}
