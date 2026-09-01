using Microsoft.AspNetCore.Components;
using SocarDispatch.Web.Models.Auth;
using SocarDispatch.Web.Models.Common;
using SocarDispatch.Web.Services;

namespace SocarDispatch.Web.Pages;

public partial class Users : ComponentBase, IDisposable
{
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    private List<UserDto> _users = new();
    private List<string> _departments = new();
    private bool _isLoading = true;
    private bool _isSavingRole = false;

    // Filters and Sorting
    private string _searchQuery = string.Empty;
    private string _selectedRoleFilter = string.Empty;
    private string _selectedDeptFilter = string.Empty;
    private string _sortBy = "name_asc";

    // Header Stats Counts
    private int _operatorCount = 0;
    private int _teamCount = 0;

    // Server-Side Pagination
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 25;
    private int _totalCount = 0;

    // Debounce CancellationTokenSource
    private CancellationTokenSource? _searchCts;

    // Edit Role Modal State
    private bool _isEditModalOpen = false;
    private UserDto? _selectedUserForEdit;
    private UpdateUserRoleRequestDto _editRoleModel = new();

    private bool HasActiveFilters => !string.IsNullOrWhiteSpace(_searchQuery) ||
                                     !string.IsNullOrWhiteSpace(_selectedRoleFilter) ||
                                     !string.IsNullOrWhiteSpace(_selectedDeptFilter);

    protected override async Task OnInitializedAsync()
    {
        await LoadUsersAsync();
        await LoadUserStatsAsync();
    }

    public async Task LoadUsersAsync(CancellationToken cancellationToken = default)
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            Guid? userId = null;
            string? searchTerm = null;

            var trimmed = _searchQuery?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                // Exact GUID lookup or keyword search
                if (Guid.TryParse(trimmed, out var parsedGuid))
                {
                    userId = parsedGuid;
                }
                else
                {
                    searchTerm = trimmed;
                }
            }

            RoleType? roleFilter = null;
            if (!string.IsNullOrWhiteSpace(_selectedRoleFilter) &&
                Enum.TryParse<RoleType>(_selectedRoleFilter, true, out var parsedRole))
            {
                roleFilter = parsedRole;
            }

            var deptFilter = string.IsNullOrWhiteSpace(_selectedDeptFilter) ? null : _selectedDeptFilter;

            var response = await UserService.GetUsersAsync(
                pageNumber: CurrentPage,
                pageSize: PageSize,
                role: roleFilter,
                department: deptFilter,
                userId: userId,
                searchTerm: searchTerm,
                cancellationToken: cancellationToken);

            if (response?.Success == true && response.Data != null)
            {
                var items = response.Data.Items;

                // Apply visual sort on current page
                _users = ApplySorting(items);
                _totalCount = response.Data.TotalCount;
                CurrentPage = response.Data.PageNumber;
            }
            else
            {
                _users.Clear();
                _totalCount = 0;
                ToastService.Show("Error", response?.Message ?? "Failed to retrieve users directory.", ToastLevel.Danger);
            }
        }
        catch (OperationCanceledException)
        {
            // Suppress cancelled search requests
            return;
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastLevel.Danger);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadUserStatsAsync()
    {
        try
        {
            var response = await UserService.GetAllUsersAsync();
            if (response?.Success == true && response.Data != null)
            {
                _operatorCount = response.Data.Count(u => u.RoleType == RoleType.Operator);
                _teamCount = response.Data.Count(u => u.RoleType == RoleType.Team);

                _departments = response.Data
                    .Where(u => !string.IsNullOrWhiteSpace(u.Department))
                    .Select(u => u.Department!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(d => d)
                    .ToList();

                StateHasChanged();
            }
        }
        catch
        {
            // Ignore background stats load error
        }
    }

    private List<UserDto> ApplySorting(IEnumerable<UserDto> items) => _sortBy switch
    {
        "name_desc" => items.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName).ToList(),
        "role" => items.OrderBy(u => u.RoleType).ThenBy(u => u.FirstName).ToList(),
        "dept" => items.OrderBy(u => u.Department).ThenBy(u => u.FirstName).ToList(),
        _ => items.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList()
    };

    private async Task HandleSearchInput(ChangeEventArgs e)
    {
        _searchQuery = e.Value?.ToString() ?? string.Empty;
        CurrentPage = 1;

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            // Debounce for 350ms before querying backend
            await Task.Delay(350, token);
            await LoadUsersAsync(token);
        }
        catch (OperationCanceledException)
        {
            // Request superseded by newer input
        }
    }

    private async Task ClearSearch()
    {
        _searchQuery = string.Empty;
        CurrentPage = 1;
        await LoadUsersAsync();
    }

    private async Task HandleRoleFilterChanged(ChangeEventArgs e)
    {
        _selectedRoleFilter = e.Value?.ToString() ?? string.Empty;
        CurrentPage = 1;
        await LoadUsersAsync();
    }

    private async Task HandleDeptFilterChanged(ChangeEventArgs e)
    {
        _selectedDeptFilter = e.Value?.ToString() ?? string.Empty;
        CurrentPage = 1;
        await LoadUsersAsync();
    }

    private void HandleSortChanged(ChangeEventArgs e)
    {
        _sortBy = e.Value?.ToString() ?? "name_asc";
        _users = ApplySorting(_users);
        StateHasChanged();
    }

    private async Task HandlePageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadUsersAsync();
    }

    private async Task HandlePageSizeChanged(int newSize)
    {
        PageSize = newSize;
        CurrentPage = 1;
        await LoadUsersAsync();
    }

    private async Task ResetFilters()
    {
        _searchQuery = string.Empty;
        _selectedRoleFilter = string.Empty;
        _selectedDeptFilter = string.Empty;
        _sortBy = "name_asc";
        CurrentPage = 1;
        await LoadUsersAsync();
    }

    // Role Management Modal
    private void OpenEditRoleModal(UserDto user)
    {
        _selectedUserForEdit = user;
        _editRoleModel = new UpdateUserRoleRequestDto
        {
            RoleType = user.RoleType,
            SubRole = user.SubRole
        };
        _isEditModalOpen = true;
    }

    private void CloseEditModal()
    {
        _isEditModalOpen = false;
        _selectedUserForEdit = null;
    }

    private async Task SaveUserRoleAsync()
    {
        if (_selectedUserForEdit == null || _isSavingRole) return;

        _isSavingRole = true;
        try
        {
            var response = await UserService.UpdateUserRoleAsync(_selectedUserForEdit.Id, _editRoleModel);
            if (response?.Success == true && response.Data != null)
            {
                _selectedUserForEdit.RoleType = response.Data.RoleType;
                _selectedUserForEdit.SubRole = response.Data.SubRole;

                ToastService.Show("Role Updated", $"Updated role for {_selectedUserForEdit.FirstName} {_selectedUserForEdit.LastName}.", ToastLevel.Success);
                CloseEditModal();
                await LoadUsersAsync();
                await LoadUserStatsAsync();
            }
            else
            {
                ToastService.Show("Update Failed", response?.Message ?? "Could not update user role.", ToastLevel.Danger);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastLevel.Danger);
        }
        finally
        {
            _isSavingRole = false;
        }
    }

    // UI Formatting Helpers
    private string GetInitials(string firstName, string lastName)
    {
        var f = !string.IsNullOrWhiteSpace(firstName) ? firstName[0].ToString().ToUpper() : "";
        var l = !string.IsNullOrWhiteSpace(lastName) ? lastName[0].ToString().ToUpper() : "";
        return $"{f}{l}";
    }

    private string GetRoleDisplayName(RoleType role) => role switch
    {
        RoleType.Operator => "Operator",
        RoleType.Team => "Team Unit",
        RoleType.Employee => "Employee",
        _ => role.ToString()
    };

    private string GetRoleBadgeClass(RoleType role) => role switch
    {
        RoleType.Operator => "badge-role-operator",
        RoleType.Team => "badge-role-team",
        RoleType.Employee => "badge-role-employee",
        _ => "badge-role-default"
    };

    private string GetRoleAvatarClass(RoleType role) => role switch
    {
        RoleType.Operator => "avatar-operator",
        RoleType.Team => "avatar-team",
        _ => "avatar-employee"
    };

    public void Dispose()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
    }
}
