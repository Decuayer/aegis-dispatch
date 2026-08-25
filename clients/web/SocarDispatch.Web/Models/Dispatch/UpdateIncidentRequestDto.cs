using System.ComponentModel.DataAnnotations;

namespace SocarDispatch.Web.Models.Dispatch;

public class UpdateIncidentRequestDto
{
    [Required(ErrorMessage = "Please select an incident category.")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select an emergency severity code.")]
    public string EmergencyCode { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    public List<CreateIncidentMediaRequestDto> MediaAttachments { get; set; } = new();

    [Required(ErrorMessage = "Latitude coordinate is required.")]
    [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
    public decimal Latitude { get; set; }

    [Required(ErrorMessage = "Longitude coordinate is required.")]
    [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
    public decimal Longitude { get; set; }

    /// <summary>
    /// Creates a populated DTO instance from an existing incident view model.
    /// </summary>
    public static UpdateIncidentRequestDto FromIncident(IncidentDetailViewModel incident)
    {
        return new UpdateIncidentRequestDto
        {
            Category = incident.Category,
            EmergencyCode = incident.EmergencyCode,
            Description = incident.Description,
            Latitude = incident.Latitude,
            Longitude = incident.Longitude,
            MediaAttachments = incident.MediaAttachments?.Select(m => new CreateIncidentMediaRequestDto
            {
                MediaUrl = m.MediaUrl,
                MediaType = m.MediaType
            }).ToList() ?? new List<CreateIncidentMediaRequestDto>()
        };
    }
}
