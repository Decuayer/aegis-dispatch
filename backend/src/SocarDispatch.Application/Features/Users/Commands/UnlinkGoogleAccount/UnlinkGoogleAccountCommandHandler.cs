using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Users.DTOs;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Users.Commands.UnlinkGoogleAccount;

public class UnlinkGoogleAccountCommandHandler : IRequestHandler<UnlinkGoogleAccountCommand, ApiResponse<CurrentUserDto>>
{
    private readonly IApplicationDbContext _context;

    public UnlinkGoogleAccountCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<CurrentUserDto>> Handle(UnlinkGoogleAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.TeamMemberships)
            .Include(u => u.LedTeams)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new EntityNotFoundException("User", request.UserId);
        }

        if (string.IsNullOrEmpty(user.GoogleId) && string.IsNullOrEmpty(user.GoogleEmail))
        {
            throw new DomainException("No Google account is currently linked to this user.");
        }

        user.GoogleId = null;
        user.GoogleEmail = null;

        await _context.SaveChangesAsync(cancellationToken);

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
            ActiveTeamId = activeTeamId,
            GoogleEmail = null
        };

        return ApiResponse<CurrentUserDto>.SuccessResult(dto, "Google account successfully unlinked.");
    }
}
