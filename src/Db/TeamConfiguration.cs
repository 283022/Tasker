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
        builder.Property(p => p.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.Name).HasColumnName("Name");
        builder.HasMany(t => t.Members)
            .WithOne()
            .HasForeignKey(tm => tm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(x => x.IdName)
            .IsRequired()
            .HasDefaultValueSql("gen_random_uuid()::text");

        builder.HasIndex(x => x.IdName)
            .IsUnique();
        
    }
}