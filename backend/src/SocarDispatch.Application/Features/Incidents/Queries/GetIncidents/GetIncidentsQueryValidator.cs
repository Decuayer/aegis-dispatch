using FluentValidation;

namespace SocarDispatch.Application.Features.Incidents.Queries.GetIncidents;

public class GetIncidentsQueryValidator : AbstractValidator<GetIncidentsQuery>
{
    private static readonly string[] AllowedSortFields = ["createdat", "status", "category", "emergencycode"];
    private static readonly string[] AllowedSortDirections = ["asc", "desc"];

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

        RuleFor(x => x.SortBy)
            .Must(s => string.IsNullOrWhiteSpace(s) || AllowedSortFields.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage("SortBy must be one of: createdAt, status, category, emergencyCode.");

        RuleFor(x => x.SortDir)
            .Must(s => string.IsNullOrWhiteSpace(s) || AllowedSortDirections.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage("SortDir must be 'asc' or 'desc'.");

        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage("ToDate must be greater than or equal to FromDate.");
        });
    }
}
