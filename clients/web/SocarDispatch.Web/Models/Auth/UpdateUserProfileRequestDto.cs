using System.ComponentModel.DataAnnotations;

namespace SocarDispatch.Web.Models.Auth;

public class UpdateUserProfileRequestDto
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    public string Department { get; set; } = string.Empty;

    public string? SubRole { get; set; }
    public string? AvatarUrl { get; set; }
}
