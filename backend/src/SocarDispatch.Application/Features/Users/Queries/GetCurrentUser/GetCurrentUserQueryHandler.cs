using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Users.DTOs;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, ApiResponse<CurrentUserDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCurrentUserQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.TeamMemberships)
            .Include(u => u.LedTeams)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new EntityNotFoundException("User", request.UserId);
        }

        // Aktif takım kimliği: Kullanıcının üyesi olduğu veya liderlik ettiği takım
        var activeTeamId = user.TeamMemberships.Select(tm => (Guid?)tm.TeamId).FirstOrDefault()
                           ?? user.LedTeams.Select(t => (Guid?)t.Id).FirstOrDefault();

        var dto = new CurrentUserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Department = user.Department,
            RoleType = user.RoleType,
            SubRole = user.SubRole,
            AvatarUrl = user.AvatarUrl,
            ActiveTeamId = activeTeamId
        };

        return ApiResponse<CurrentUserDto>.SuccessResult(dto, "User information successfully retrieved.");
    }
}
