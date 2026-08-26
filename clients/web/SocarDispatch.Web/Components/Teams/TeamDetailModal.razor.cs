using Microsoft.AspNetCore.Components;
using SocarDispatch.Web.Models.Dispatch;
using SocarDispatch.Web.Services;

namespace SocarDispatch.Web.Components.Teams;

public partial class TeamDetailModal : ComponentBase
{
    [Parameter]
    public bool IsVisible { get; set; }

    [Parameter]
    public TeamDto? Team { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<TeamDto> OnTeamChanged { get; set; }

    private bool _isEditMode;
    private bool _isUpdatingStatus;

    protected override void OnParametersSet()
    {
        if (!IsVisible)
        {
            _isEditMode = false;
        }
    }

    private void ToggleEditMode()
    {
        _isEditMode = !_isEditMode;
    }

    private async Task HandleClose()
    {
        _isEditMode = false;
        if (OnClose.HasDelegate)
        {
            await OnClose.InvokeAsync();
        }
    }

    private async Task HandleTeamSaved(TeamDto updatedTeam)
    {
        _isEditMode = false;
        if (OnTeamChanged.HasDelegate)
        {
            await OnTeamChanged.InvokeAsync(updatedTeam);
        }
    }

    private async Task HandleStatusOverride(ChangeEventArgs e)
    {
        if (Team == null || _isUpdatingStatus) return;

        var newStatus = e.Value?.ToString();
        if (string.IsNullOrWhiteSpace(newStatus) || newStatus == Team.Status) return;

        _isUpdatingStatus = true;
        try
        {
            var response = await TeamService.UpdateTeamStatusAsync(Team.Id, newStatus);
            if (response != null && response.Success && response.Data != null)
            {
                Team.Status = response.Data.Status;
                Team.UpdatedAt = response.Data.UpdatedAt;
                ToastService.ShowSuccess($"Team status changed to {newStatus}.");

                if (OnTeamChanged.HasDelegate)
                {
                    await OnTeamChanged.InvokeAsync(response.Data);
                }
            }
            else
            {
                ToastService.ShowError(response?.Message ?? "Failed to change team status.");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Status update error: {ex.Message}");
        }
        finally
        {
            _isUpdatingStatus = false;
        }
    }

    private void NavigateToMap()
    {
        Navigation.NavigateTo("map");
    }

    private string GetStatusBadgeClass(string? status) => status switch
    {
        "Idle" => "badge-status-idle",
        "Forwarded" => "badge-status-forwarded",
        "OnScene" => "badge-status-onscene",
        "Busy" => "badge-status-busy",
        _ => "badge-status-default"
    };

    private string GetMemberStatusBadgeClass(string? status) => status switch
    {
        "Available" => "badge-code-green",
        "EnRoute" => "badge-code-orange",
        "OnScene" => "badge-code-yellow",
        "Unavailable" => "badge-code-red",
        _ => "badge-code-default"
    };

    private string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "T";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0][0].ToString().ToUpper();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
    }
}
