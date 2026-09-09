using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Users.DTOs;

public class CurrentUserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? PhoneNumber => Phone;
    public string Department { get; set; } = string.Empty;
    public RoleType RoleType { get; set; }
    public string? SubRole { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? ActiveTeamId { get; set; }
    public string? GoogleEmail { get; set; }
    public bool IsGoogleLinked => !string.IsNullOrWhiteSpace(GoogleEmail);
}
