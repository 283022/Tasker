namespace src.Domain.Entities;

public class Session
{
    public Guid Id { get; set; }

    // Группирует последовательные refresh-токены одного входа.
    //TODO: Задумать над Family ID

    public Guid UserId { get; set; }

    // Храним только хеш refresh token.
    public string RefreshTokenHash { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

}