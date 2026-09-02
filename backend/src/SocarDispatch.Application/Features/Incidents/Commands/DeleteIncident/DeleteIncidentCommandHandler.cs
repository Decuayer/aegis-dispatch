using MediatR;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Application.Common.Models;
using SocarDispatch.Domain.Enums;
using SocarDispatch.Domain.Exceptions;

namespace SocarDispatch.Application.Features.Incidents.Commands.DeleteIncident;

public class DeleteIncidentCommandHandler : IRequestHandler<DeleteIncidentCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteIncidentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteIncidentCommand request, CancellationToken cancellationToken)
    {
        // Query filter automatically excludes records where IsDeleted == true
        var incident = await _context.Incidents
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (incident == null)
        {
            throw new EntityNotFoundException("Incident", request.Id);
        }

        var requester = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.RequesterId, cancellationToken);

        var isOperator = requester?.RoleType == RoleType.Operator || request.UserRole == "Operator";
        var isOwner = incident.ReporterId == request.RequesterId;

        if (!isOperator && !isOwner)
        {
            throw new ForbiddenAccessException("You do not have permission to delete this incident.");
        }

        if (!isOperator && isOwner && incident.Status != IncidentStatus.Open)
        {
            throw new DomainException("Reporters can only delete incidents while they are in open status.");
        }

        // Soft-delete: Do not physically purge from database
        incident.IsDeleted = true;
        incident.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResult(true, "Incident deleted successfully.");
    }
}
