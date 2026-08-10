using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities;

namespace src.Db;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.HasKey(t => new {t.TeamId, t.UserId}).HasName("PK_TeamMember");
        builder.ToTable("TeamMembers");
        builder.Property(t => t.IsAdmin).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId);
        builder.HasOne<Team>().WithMany().HasForeignKey(t => t.TeamId);
    }
}