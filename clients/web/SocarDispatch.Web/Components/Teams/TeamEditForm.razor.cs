using Microsoft.AspNetCore.Components;
using SocarDispatch.Web.Models.Auth;
using SocarDispatch.Web.Models.Dispatch;
using SocarDispatch.Web.Services;

namespace SocarDispatch.Web.Components.Teams;

public partial class TeamEditForm : ComponentBase
{
    [Parameter, EditorRequired]
    public TeamDto Team { get; set; } = default!;

    [Parameter]
    public EventCallback<TeamDto> OnSaved { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private string _teamName = string.Empty;
    private string _selectedLeaderId = string.Empty;
    private string _selectedStatus = "Idle";
    private string _searchCandidateTerm = string.Empty;

    private bool _isSubmitting;
    private bool _isUpdatingStatus;
    private bool _isRosterProcessing;
    private Guid? _confirmDeleteUserId;

    private List<UserDto> _availableUsers = new();

    private List<UserDto> CandidateUsers => _availableUsers
        .Where(u => Team != null && !Team.Members.Any(m => m.UserId == u.Id))
        .Where(u => string.IsNullOrWhiteSpace(_searchCandidateTerm) ||
                    $"{u.FirstName} {u.LastName}".Contains(_searchCandidateTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.Department.Contains(_searchCandidateTerm, StringComparison.OrdinalIgnoreCase))
        .Take(5)
        .ToList();

    protected override async Task OnParametersSetAsync()
    {
        if (Team != null)
        {
            _teamName = Team.TeamName;
            _selectedLeaderId = Team.LeaderId.HasValue && Team.Members.Any(m => m.UserId == Team.LeaderId.Value)
                ? Team.LeaderId.Value.ToString()
                : string.Empty;
            _selectedStatus = Team.Status;

            if (_availableUsers.Count == 0)
            {
                await LoadAvailableUsers();
            }
        }

    }

    private async Task LoadAvailableUsers()
    {
        try
        {
            var response = await UserService.GetUsersAsync(role: RoleType.Team);
            if (response != null && response.Success && response.Data != null)
            {
                _availableUsers = response.Data;
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to load personnel: {ex.Message}");
        }
    }


    private async Task HandleSaveTeam()
    {
        if (Team == null || string.IsNullOrWhiteSpace(_teamName)) return;

        _isSubmitting = true;
        try
        {
            Guid? leaderGuid = Guid.TryParse(_selectedLeaderId, out var parsedGuid) ? parsedGuid : null;
            var request = new UpdateTeamRequestDto
            {
                TeamName = _teamName.Trim(),
                LeaderId = leaderGuid
            };

            var response = await TeamService.UpdateTeamAsync(Team.Id, request);
            if (response != null && response.Success && response.Data != null)
            {
                Team.TeamName = response.Data.TeamName;
                Team.LeaderId = response.Data.LeaderId;
                Team.LeaderFullName = response.Data.LeaderFullName;
                Team.UpdatedAt = response.Data.UpdatedAt;

                ToastService.ShowSuccess("Team information saved successfully.");

                if (OnSaved.HasDelegate)
                {
                    await OnSaved.InvokeAsync(Team);
                }
            }
            else
            {
                ToastService.ShowError(response?.Message ?? "Failed to save team information.");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Update error: {ex.Message}");
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task ChangeTeamStatus(string newStatus)
    {
        if (Team == null || _isUpdatingStatus || string.Equals(_selectedStatus, newStatus, StringComparison.OrdinalIgnoreCase)) return;

        _isUpdatingStatus = true;
        try
        {
            var response = await TeamService.UpdateTeamStatusAsync(Team.Id, newStatus);
            if (response != null && response.Success && response.Data != null)
            {
                _selectedStatus = response.Data.Status;
                Team.Status = response.Data.Status;
                Team.UpdatedAt = response.Data.UpdatedAt;
                ToastService.ShowSuccess($"Team status updated to {newStatus}.");
            }
            else
            {
                ToastService.ShowError(response?.Message ?? "Failed to update team status.");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Status error: {ex.Message}");
        }
        finally
        {
            _isUpdatingStatus = false;
        }
    }

    private async Task AddMember(Guid userId)
    {
        if (Team == null || _isRosterProcessing) return;

        _isRosterProcessing = true;
        try
        {
            var response = await TeamService.AddMemberAsync(Team.Id, userId);
            if (response != null && response.Success && response.Data != null)
            {
                Team.Members = response.Data.Members;
                Team.UpdatedAt = response.Data.UpdatedAt;
                _searchCandidateTerm = string.Empty;
                ToastService.ShowSuccess("Member added to team roster.");
            }
            else
            {
                ToastService.ShowError(response?.Message ?? "Failed to add team member.");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Add member error: {ex.Message}");
        }
        finally
        {
            _isRosterProcessing = false;
        }
    }

    private void PromptRemoveMember(Guid userId)
    {
        _confirmDeleteUserId = userId;
    }

    private void CancelRemoveMember()
    {
        _confirmDeleteUserId = null;
    }

    private async Task ConfirmRemoveMember(Guid userId)
    {
        if (Team == null || _isRosterProcessing) return;

        if (userId == Team.LeaderId)
        {
            ToastService.ShowWarning("Cannot remove designated team leader. Reassign leadership first.");
            _confirmDeleteUserId = null;
            return;
        }

        _isRosterProcessing = true;
        try
        {
            var response = await TeamService.RemoveMemberAsync(Team.Id, userId);
            if (response != null && response.Success && response.Data != null)
            {
                Team.Members = response.Data.Members;
                Team.UpdatedAt = response.Data.UpdatedAt;
                _confirmDeleteUserId = null;
                if (_selectedLeaderId == userId.ToString())
                {
                    _selectedLeaderId = string.Empty;
                }
                ToastService.ShowSuccess("Member removed from team roster.");
            }
            else
            {
                ToastService.ShowError(response?.Message ?? "Failed to remove member.");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Remove error: {ex.Message}");
        }
        finally
        {
            _isRosterProcessing = false;
        }
    }

    private async Task HandleMemberStatusChange(Guid userId, ChangeEventArgs e)
    {
        if (Team == null || _isRosterProcessing) return;

        var newStatus = e.Value?.ToString();
        if (string.IsNullOrWhiteSpace(newStatus)) return;

        _isRosterProcessing = true;
        try
        {
            var response = await TeamService.UpdateMemberStatusAsync(Team.Id, userId, newStatus);
            if (response != null && response.Success && response.Data != null)
            {
                var member = Team.Members.FirstOrDefault(m => m.UserId == userId);
                if (member != null)
                {
                    member.MemberStatus = response.Data.MemberStatus;
                    member.StatusUpdatedAt = response.Data.StatusUpdatedAt;
                }
                ToastService.ShowSuccess($"Member status updated to {newStatus}.");
            }
            else
            {
                ToastService.ShowError(response?.Message ?? "Failed to update member status.");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Status update error: {ex.Message}");
        }
        finally
        {
            _isRosterProcessing = false;
        }
    }

    private async Task HandleCancel()
    {
        if (OnCancel.HasDelegate)
        {
            await OnCancel.InvokeAsync();
        }
    }

    private string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "R";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0][0].ToString().ToUpper();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
    }
}
