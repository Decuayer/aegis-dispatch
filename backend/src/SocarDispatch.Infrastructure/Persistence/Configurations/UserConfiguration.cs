using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocarDispatch.Domain.Entities;

namespace SocarDispatch.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Phone).HasMaxLength(20).IsRequired();
        builder.HasIndex(u => u.Phone).IsUnique();
        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(u => u.Department).HasMaxLength(100).IsRequired();
        builder.Property(u => u.RoleType).HasMaxLength(20).IsRequired();
        builder.Property(u => u.SubRole).HasMaxLength(50);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.DeviceToken).HasMaxLength(500);
        builder.Property(u => u.DeviceTokenUpdatedAt);
        builder.Property(u => u.GoogleId).HasMaxLength(100);
        builder.HasIndex(u => u.GoogleId).IsUnique();
        builder.Property(u => u.GoogleEmail).HasMaxLength(255);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");

        // Composite index for user directory filtering and role-based lookups
        builder.HasIndex(u => new { u.RoleType, u.Department, u.FirstName, u.LastName })
            .HasDatabaseName("IX_Users_RoleType_Department_FirstName_LastName");
    }
}
