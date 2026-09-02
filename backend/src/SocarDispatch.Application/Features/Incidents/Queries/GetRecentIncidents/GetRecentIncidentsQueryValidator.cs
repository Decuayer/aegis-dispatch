using FluentValidation;

namespace SocarDispatch.Application.Features.Incidents.Queries.GetRecentIncidents;

public class GetRecentIncidentsQueryValidator : AbstractValidator<GetRecentIncidentsQuery>
{
    public GetRecentIncidentsQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .WithMessage("Limit must be between 1 and 50.");
    }
}
