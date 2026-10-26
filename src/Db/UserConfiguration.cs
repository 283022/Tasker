using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using src.Domain.Entities;

namespace src.Db;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Первичный ключ
        builder.HasKey(x => x.Id)
            .HasName("PK_User");

        builder.Property(p => p.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");
        
        builder.HasMany<TeamMember>()
            .WithOne(tm => tm.User)
            .HasForeignKey(tm => tm.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Настройка Complex Property для Email
        builder.ComplexProperty(x => x.Email, email =>
        {
            email.Property(e => e.Value)
                .IsRequired()
                .HasColumnName("Email_Value")
                .HasMaxLength(254);
        });
        
        // Настройка Complex Property для Password
        builder.ComplexProperty(x => x.Password, password =>
        {
            password.Property(e => e.HashPassword)
                .IsRequired()
                .HasColumnName("Password_Value")
                .HasMaxLength(256);
        });

        // Обычное свойство Name
        builder.Property(x => x.Name)
            .IsRequired()
            .HasColumnName("Name_Value")
            .HasMaxLength(254);

        // Check Constraint для Email (regex)
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_User_Email_Valid",
            "\"Email_Value\" ~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'"));

        // Check Constraint для Password (длина)
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_User_Password_Length",
            "LENGTH(\"Password_Value\") >= 8"));

        // Явное имя таблицы (опционально)
        builder.ToTable("Users");
        
        builder.Property(x => x.DisplayName)
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValueSql("replace(gen_random_uuid()::text, '-', '')");

        builder.HasIndex(x => x.DisplayName)
            .IsUnique();
    }
}