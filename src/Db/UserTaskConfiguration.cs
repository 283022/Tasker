using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities;

namespace src.Db;

public class UserTaskConfiguration : IEntityTypeConfiguration<UserTask>
{
    public void Configure(EntityTypeBuilder<UserTask> builder)
    {
        // Первичный ключ
        builder.HasKey(x => x.Id)
            .HasName("PK_UserTask");

        builder.Property(p => p.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");
        // Свойство Name
        builder.Property(x => x.Name)
            .IsRequired()
            .HasColumnName("Name_Value")
            .HasMaxLength(254);

        // Внешний ключ к User (без навигационного свойства, как у тебя)
        builder.Property(x => x.CreateByUserId)
            .IsRequired();

        // Связь с User (один UserTask → один User)
        builder.HasOne<User>()
            .WithMany()                    // У User нет коллекции UserTasks
            .HasForeignKey(x => x.CreateByUserId)
            .OnDelete(DeleteBehavior.Cascade); // Если User удалён — удалить его Task'и

        // Явное имя таблицы
        builder.ToTable("UserTasks");
        
        
        builder.Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

        builder.Property(t => t.MarkdownText)
                .HasColumnType("text");

        builder.Property(t => t.DueAt)
                .HasColumnType("timestamp with time zone");

        builder.Property(t => t.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

        builder.Property(t => t.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

        builder.HasIndex(t => t.DueAt);
        
    }
}