using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Application.Features.Auth.DTOs;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Users.Commands.UpdateUserRole;

public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, ApiResponse<UserDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserRoleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<UserDto>> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (request.OperatorId == request.UserId && request.RoleType != RoleType.Operator)
        {
            throw new DomainException("Operators cannot demote their own account.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new EntityNotFoundException("User", request.UserId);
        }

        if (user.RoleType == RoleType.Team && request.RoleType == RoleType.Employee)
        {
            var isAssignedToActiveDispatch = await _context.Assignments
                .Include(a => a.Incident)
                .AnyAsync(a =>
                    (a.Team.LeaderId == request.UserId || a.Team.Members.Any(m => m.UserId == request.UserId)) &&
                    a.CompletedAt == null &&
                    a.Incident.Status != IncidentStatus.Resolved &&
                    a.Incident.Status != IncidentStatus.Canceled,
                    cancellationToken);

            if (isAssignedToActiveDispatch)
            {
                throw new DomainException("Cannot demote user from Team role while currently assigned to an active field dispatch.");
            }
        }

        user.RoleType = request.RoleType;
        user.SubRole = request.SubRole;

        await _context.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Department = user.Department,
            RoleType = user.RoleType,
            SubRole = user.SubRole,
            AvatarUrl = user.AvatarUrl
        };

        return ApiResponse<UserDto>.SuccessResult(userDto, "User role updated successfully.");
    }
}
