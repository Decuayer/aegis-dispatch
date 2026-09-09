using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Users.DTOs;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Users.Commands.LinkGoogleAccount;

public class LinkGoogleAccountCommandHandler : IRequestHandler<LinkGoogleAccountCommand, ApiResponse<CurrentUserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IGoogleAuthService _googleAuthService;

    public LinkGoogleAccountCommandHandler(
        IApplicationDbContext context,
        IGoogleAuthService googleAuthService)
    {
        _context = context;
        _googleAuthService = googleAuthService;
    }

    public async Task<ApiResponse<CurrentUserDto>> Handle(LinkGoogleAccountCommand request, CancellationToken cancellationToken)
    {
        var googleUser = await _googleAuthService.VerifyIdTokenAsync(request.IdToken, cancellationToken);
        var emailNormalized = googleUser.Email.Trim().ToLowerInvariant();

        // Check if this Google account is already linked to another user
        var alreadyLinkedUser = await _context.Users
            .AnyAsync(u => u.Id != request.UserId &&
                           (u.GoogleId == googleUser.GoogleId ||
                            (u.GoogleEmail != null && u.GoogleEmail.ToLower() == emailNormalized)),
                      cancellationToken);

        if (alreadyLinkedUser)
        {
            throw new DomainException("This Google account is already linked to another user.");
        }

        var user = await _context.Users
            .Include(u => u.TeamMemberships)
            .Include(u => u.LedTeams)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new EntityNotFoundException("User", request.UserId);
        }

        user.GoogleId = googleUser.GoogleId;
        user.GoogleEmail = googleUser.Email;

        if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(googleUser.PictureUrl))
        {
            user.AvatarUrl = googleUser.PictureUrl;
        }

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
            GoogleEmail = user.GoogleEmail
        };

        return ApiResponse<CurrentUserDto>.SuccessResult(dto, "Google account successfully linked.");
    }
}
