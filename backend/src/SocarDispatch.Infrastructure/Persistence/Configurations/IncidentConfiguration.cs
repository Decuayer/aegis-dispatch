using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Infrastructure.Persistence.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("Incidents");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Category).HasMaxLength(50).IsRequired();
        builder.Property(i => i.EmergencyCode).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Description).HasColumnType("text");
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(IncidentStatus.Open).IsRequired();

        builder.Property(t => t.Latitude).HasPrecision(9, 6);
        builder.Property(t => t.Longitude).HasPrecision(9, 6);

        // PostGIS Geometry Point and GiST Spatial Index
        builder.Property(i => i.Location).HasColumnType("geometry(Point, 4326)").IsRequired();
        builder.HasIndex(i => i.Location).HasMethod("GIST");

        builder.Property(i => i.CreatedAt).HasDefaultValueSql("NOW()");

        // Composite index for chronological sorting and status/category filtering
        builder.HasIndex(i => new { i.CreatedAt, i.Status, i.Category })
            .IsDescending(true, false, false)
            .HasDatabaseName("IX_Incidents_CreatedAt_Status_Category");

        builder.HasOne(i => i.Reporter)
            .WithMany(u => u.ReportedIncidents)
            .HasForeignKey(i => i.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
