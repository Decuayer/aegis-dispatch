namespace SocarDispatch.Web.Models.Dispatch;

public class CreateTeamRequestDto
{
    public string TeamName { get; set; } = string.Empty;
    public Guid? LeaderId { get; set; }
}

public class UpdateTeamRequestDto
{
    public string TeamName { get; set; } = string.Empty;
    public Guid? LeaderId { get; set; }
}

public class UpdateTeamStatusRequestDto
{
    public string Status { get; set; } = string.Empty;
}

public class AddTeamMemberRequestDto
{
    public Guid UserId { get; set; }
}

public class UpdateMemberStatusRequestDto
{
    public string Status { get; set; } = string.Empty;
}
