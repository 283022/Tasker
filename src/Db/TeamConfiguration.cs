using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities;

namespace src.Db;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasColumnName("Name");
        builder.HasMany<TeamMember>().WithOne().HasForeignKey(t => t.TeamId);
    }
}