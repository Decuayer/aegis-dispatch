using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocarDispatch.Domain.Entities;

namespace SocarDispatch.Infrastructure.Persistence.Configurations;

public class EmergencyCodeConfiguration : IEntityTypeConfiguration<EmergencyCodeDefinition>
{
    public void Configure(EntityTypeBuilder<EmergencyCodeDefinition> builder)
    {
        builder.ToTable("Emergency_Codes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.Property(c => c.ColorHex)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(250);

        builder.Property(c => c.SeverityLevel)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("NOW()");

        // Seed Default Emergency Codes
        builder.HasData(
            new EmergencyCodeDefinition
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Code = "Red",
                ColorHex = "#FF3B30",
                SeverityLevel = 1,
                Description = "Critical emergency requiring immediate dispatch",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new EmergencyCodeDefinition
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Code = "Yellow",
                ColorHex = "#FFCC00",
                SeverityLevel = 2,
                Description = "High-risk incident requiring prompt response",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new EmergencyCodeDefinition
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Code = "Green",
                ColorHex = "#34C759",
                SeverityLevel = 3,
                Description = "Low-risk/informational report",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
