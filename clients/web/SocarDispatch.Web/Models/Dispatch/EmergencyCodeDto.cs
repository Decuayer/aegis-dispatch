using System.ComponentModel.DataAnnotations;

namespace SocarDispatch.Web.Models.Dispatch;

public class EmergencyCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SeverityLevel { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateEmergencyCodeRequestDto
{
    [Required(ErrorMessage = "Emergency code is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 50 characters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Color hex is required.")]
    [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Invalid hex color format (e.g. #FF0000).")]
    public string ColorHex { get; set; } = "#EF4444";

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 10, ErrorMessage = "Severity level must be between 1 and 10.")]
    public int SeverityLevel { get; set; } = 1;
}

public class UpdateEmergencyCodeRequestDto
{
    [Required(ErrorMessage = "Emergency code is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 50 characters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Color hex is required.")]
    [RegularExpression("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Invalid hex color format (e.g. #FF0000).")]
    public string ColorHex { get; set; } = "#EF4444";

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 10, ErrorMessage = "Severity level must be between 1 and 10.")]
    public int SeverityLevel { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
