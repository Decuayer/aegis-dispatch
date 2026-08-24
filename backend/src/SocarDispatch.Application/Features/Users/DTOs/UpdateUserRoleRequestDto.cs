using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Application.Features.Users.DTOs;

public class UpdateUserRoleRequestDto
{
    public RoleType RoleType { get; set; }
    public string? SubRole { get; set; }
}
