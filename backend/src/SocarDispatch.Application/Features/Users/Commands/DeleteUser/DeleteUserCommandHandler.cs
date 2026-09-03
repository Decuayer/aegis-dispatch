using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
        {
            throw new EntityNotFoundException("User", request.Id);
        }

        if (user.Id == request.RequesterId)
        {
            throw new DomainException("Operators cannot delete their own account.");
        }

        // Check if user has associated incident reports
        var hasIncidents = await _context.Incidents
            .AnyAsync(i => i.ReporterId == user.Id, cancellationToken);

        if (hasIncidents)
        {
            throw new DomainException("Cannot delete this user because they are associated with existing incident reports.");
        }

        // Check if user has associated dispatch assignments
        var hasAssignments = await _context.Assignments
            .AnyAsync(a => a.OperatorId == user.Id, cancellationToken);

        if (hasAssignments)
        {
            throw new DomainException("Cannot delete this user because they are associated with dispatch assignments.");
        }

        // Detach team leadership
        var ledTeams = await _context.Teams
            .Where(t => t.LeaderId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var team in ledTeams)
        {
            team.LeaderId = null;
        }

        // Detach team memberships
        var memberships = await _context.TeamMembers
            .Where(tm => tm.UserId == user.Id)
            .ToListAsync(cancellationToken);

        if (memberships.Count > 0)
        {
            _context.TeamMembers.RemoveRange(memberships);
        }

        // Remove feedback entries submitted by user
        var feedbacks = await _context.Feedbacks
            .Where(f => f.UserId == user.Id)
            .ToListAsync(cancellationToken);

        if (feedbacks.Count > 0)
        {
            _context.Feedbacks.RemoveRange(feedbacks);
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResult(true, "User deleted successfully.");
    }
}
