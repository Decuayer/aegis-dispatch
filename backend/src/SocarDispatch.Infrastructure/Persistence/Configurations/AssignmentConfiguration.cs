using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocarDispatch.Domain.Entities;

namespace SocarDispatch.Infrastructure.Persistence.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.AssignedAt).HasDefaultValueSql("NOW()");

        builder.HasOne(a => a.Incident)
            .WithMany(i => i.Assignments)
            .HasForeignKey(a => a.IncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Team)
            .WithMany(t => t.Assignments)
            .HasForeignKey(a => a.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Operator)
            .WithMany(u => u.AssignedDispatches)
            .HasForeignKey(a => a.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
