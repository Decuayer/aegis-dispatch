using FluentValidation;

namespace SocarDispatch.Application.Features.Teams.Queries.GetTeams;

public class GetTeamsQueryValidator : AbstractValidator<GetTeamsQuery>
{
    public GetTeamsQueryValidator()
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
    }
}
