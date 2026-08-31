using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;

namespace SocarDispatch.Infrastructure.Persistence.Configurations;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("Feedbacks");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(f => f.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(FeedbackStatus.Pending)
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(f => f.UpdatedAt);

        // Indexes for performance
        builder.HasIndex(f => f.UserId);
        builder.HasIndex(f => f.Status);
        builder.HasIndex(f => f.CreatedAt);
        builder.HasIndex(f => new { f.UserId, f.Status, f.CreatedAt });

        // Restrict delete: Preserves feedback history if user state changes or user is deleted
        builder.HasOne(f => f.User)
            .WithMany(u => u.Feedbacks)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
