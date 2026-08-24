namespace SocarDispatch.Web.Models.Auth;

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public RoleType RoleType { get; set; }
    public string? SubRole { get; set; }
    public string? AvatarUrl { get; set; }
}
