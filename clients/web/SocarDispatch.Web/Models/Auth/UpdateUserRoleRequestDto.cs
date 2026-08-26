// Models/Auth/UpdateUserRoleRequestDto.cs
namespace SocarDispatch.Web.Models.Auth;

public class UpdateUserRoleRequestDto
{
    public RoleType RoleType { get; set; }
    public string? SubRole { get; set; }
}
