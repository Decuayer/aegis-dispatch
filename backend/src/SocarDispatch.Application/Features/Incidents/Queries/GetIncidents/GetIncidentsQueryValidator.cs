using FluentValidation;

namespace SocarDispatch.Application.Features.Incidents.Queries.GetIncidents;

public class GetIncidentsQueryValidator : AbstractValidator<GetIncidentsQuery>
{
    public GetIncidentsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .WithMessage("Search term cannot exceed 100 characters.");

        RuleFor(x => x.Category)
            .MaximumLength(50)
            .WithMessage("Category cannot exceed 50 characters.");

        RuleFor(x => x.EmergencyCode)
            .MaximumLength(20)
            .WithMessage("Emergency code cannot exceed 20 characters.");

        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage("ToDate must be greater than or equal to FromDate.");
        });
    }
}
