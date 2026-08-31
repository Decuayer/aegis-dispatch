using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Domain.Entities;

namespace SocarDispatch.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<IncidentMedia> IncidentMedia => Set<IncidentMedia>();
    public DbSet<EmergencyCodeDefinition> EmergencyCodes => Set<EmergencyCodeDefinition>();
    public DbSet<IncidentReport> IncidentReports => Set<IncidentReport>();
    public DbSet<IncidentCategory> IncidentCategories => Set<IncidentCategory>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<FeedbackMedia> FeedbackMedia => Set<FeedbackMedia>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Report the PostGIS extension to EF Core
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("uuid-ossp");

        // Automatically apply all IEntityTypeConfiguration classes
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
