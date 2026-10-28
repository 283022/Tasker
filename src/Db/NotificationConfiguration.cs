using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities;

namespace src.Db;

public class NotificationConfiguration
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.Type)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(n => n.Message)
            .HasMaxLength(500)
            .IsRequired();

        
        builder.HasIndex(n => new { n.UserId, n.CreatedAt });   
    }
    
}