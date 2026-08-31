using Microsoft.EntityFrameworkCore;
using SocarDispatch.Domain.Entities;

namespace SocarDispatch.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Team> Teams { get; }
    DbSet<TeamMember> TeamMembers { get; }
    DbSet<Incident> Incidents { get; }
    DbSet<Assignment> Assignments { get; }
    DbSet<IncidentMedia> IncidentMedia { get; }
    DbSet<EmergencyCodeDefinition> EmergencyCodes { get; }
    DbSet<IncidentReport> IncidentReports { get; }
    DbSet<IncidentCategory> IncidentCategories { get; }
    DbSet<Feedback> Feedbacks { get; }
    DbSet<FeedbackMedia> FeedbackMedia { get; }


    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
