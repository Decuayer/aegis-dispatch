using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;


namespace SocarDispatch.Infrastructure.Persistence.Configurations;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("Team_Members");

        builder.HasKey(tm => new { tm.TeamId, tm.UserId });

        builder.HasOne(tm => tm.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(tm => tm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tm => tm.User)
            .WithMany(u => u.TeamMemberships)
            .HasForeignKey(tm => tm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(tm => tm.MemberStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(TeamMemberStatus.Available)
            .IsRequired();
        builder.Property(tm => tm.JoinedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();
    }
}
