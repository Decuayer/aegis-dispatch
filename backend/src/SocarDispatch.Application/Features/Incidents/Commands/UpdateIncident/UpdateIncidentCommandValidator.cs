using FluentValidation;

namespace SocarDispatch.Application.Features.Incidents.Commands.UpdateIncident;

public class UpdateIncidentCommandValidator : AbstractValidator<UpdateIncidentCommand>
{
    private static readonly string[] AllowedCategories =
    {
        "Fire",
        "Medical",
        "Security",
        "Environmental",
        "Chemical"
    };

    public UpdateIncidentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequesterId).NotEmpty();

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Incident category is required.")
            .MaximumLength(50).WithMessage("Category cannot exceed 50 characters.")
            .Must(c => AllowedCategories.Contains(c))
            .WithMessage($"Invalid category. Allowed values: {string.Join(", ", AllowedCategories)}");

        RuleFor(x => x.EmergencyCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m);
    }
}
