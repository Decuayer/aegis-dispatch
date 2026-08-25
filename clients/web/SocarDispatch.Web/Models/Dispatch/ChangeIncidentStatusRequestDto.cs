using System.ComponentModel.DataAnnotations;

namespace SocarDispatch.Web.Models.Dispatch;

public class ChangeIncidentStatusRequestDto
{
    [Required(ErrorMessage = "Status value is required.")]
    public string Status { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Completion notes cannot exceed 2000 characters.")]
    public string? CompletionNotes { get; set; }
}
