namespace src.Domain.Entities;

public class Notification
{
    //for ef core
    private Notification() { }

    private Notification(
        Guid userId,
        NotificationType type,
        string message,
        Guid? taskId,
        Guid? teamId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Type = type;
        Message = message;
        TaskId = taskId;
        TeamId = teamId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string? Message { get; private set; }
    public Guid? TaskId { get; private set; }
    public Guid? TeamId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    public static Result<Notification> Create(
        Guid userId,
        NotificationType type,
        string message,
        Guid? taskId = null,
        Guid? teamId = null)
    {
        if (userId == Guid.Empty)
             return Errors.Validation("User id cannot be empty");

        if (string.IsNullOrWhiteSpace(message))
            return Errors.Validation("Message cannot be empty");
        
        if (message.Length > 500)
            return Errors.Validation("Message length cannot be more than 500 characters");
        
        if (type.ToString().Length > 64 )
            return Errors.Validation("Type length cannot be more than 64 characters");
        
        return new Notification(
            userId,
            type,
            message,
            taskId,
            teamId);
    }

    public void MarkAsRead()
    {
        ReadAt ??= DateTimeOffset.UtcNow;
    }
}