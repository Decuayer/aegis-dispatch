using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Infrastructure.Persistence.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.TeamName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(TeamStatus.Idle).IsRequired();

        builder.Property(t => t.CurrentLatitude).HasPrecision(9, 6);
        builder.Property(t => t.CurrentLongitude).HasPrecision(9, 6);

        // PostGIS Geometry Point (WGS84 - SRID 4326) and GiST Spatial Index
        builder.Property(t => t.Location).HasColumnType("geometry(Point, 4326)");
        builder.HasIndex(t => t.Location).HasMethod("GIST");

        builder.Property(t => t.UpdatedAt).HasDefaultValueSql("NOW()");

        // Composite index for team availability and name search
        builder.HasIndex(t => new { t.Status, t.TeamName })
            .HasDatabaseName("IX_Teams_Status_TeamName");

        // Team Leader (User -> Teams 1-to-Many optional relationship)
        builder.HasOne(t => t.Leader)
            .WithMany(u => u.LedTeams)
            .HasForeignKey(t => t.LeaderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
