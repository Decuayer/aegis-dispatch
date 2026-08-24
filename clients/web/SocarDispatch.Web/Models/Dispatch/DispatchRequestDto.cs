using System.ComponentModel.DataAnnotations;

namespace SocarDispatch.Web.Models.Dispatch;

/// The request DTO sent by the operator when assigning a team to an emergency incident.
public class DispatchRequestDto
{
    /// Unique identifier (ID) of the emergency incident to be addressed.
    [Required(ErrorMessage = "Event selection is mandatory.")]
    public Guid IncidentId { get; set; }

    
    /// ID of the emergency response team to be dispatched to the scene.
    [Required(ErrorMessage = "Selecting the team to be dispatched is mandatory.")]
    public Guid TeamId { get; set; }

    /// Optional operational instructions or notes that the operator wishes to convey to the team during dispatch.
    [MaxLength(500, ErrorMessage = "The note can be a maximum of 500 characters.")]
    public string? Notes { get; set; }
}
