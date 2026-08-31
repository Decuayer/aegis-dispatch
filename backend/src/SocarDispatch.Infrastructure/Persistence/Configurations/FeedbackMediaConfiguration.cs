using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocarDispatch.Domain.Entities;

namespace SocarDispatch.Infrastructure.Persistence.Configurations;

public class FeedbackMediaConfiguration : IEntityTypeConfiguration<FeedbackMedia>
{
    public void Configure(EntityTypeBuilder<FeedbackMedia> builder)
    {
        builder.ToTable("Feedback_Media");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MediaUrl)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(m => m.MediaType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasDefaultValueSql("NOW()");

        // Cascade delete: Removing a feedback automatically purges associated media references
        builder.HasOne(m => m.Feedback)
            .WithMany(f => f.MediaAttachments)
            .HasForeignKey(m => m.FeedbackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
