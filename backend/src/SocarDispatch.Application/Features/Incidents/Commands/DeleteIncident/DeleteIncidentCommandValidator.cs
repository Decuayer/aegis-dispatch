using FluentValidation;

namespace SocarDispatch.Application.Features.Incidents.Commands.DeleteIncident;

public class DeleteIncidentCommandValidator : AbstractValidator<DeleteIncidentCommand>
{
    public DeleteIncidentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Incident ID is required.");

        RuleFor(x => x.RequesterId)
            .NotEmpty().WithMessage("Requester ID is required.");
    }
}
