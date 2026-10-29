
namespace src.Domain.Entities;

public class UserTask
{
    public const int MaxNameLength = 200;
    public const int MaxMarkdownLength = 20_000;

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? MarkdownText { get; private set; }

    public Guid CreateByUserId { get; private set; }

    public Guid? AssignedToUserId { get; private set; }

    public Guid? TeamId { get; private set; }

    public UserTaskStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DueAt { get; private set; }

    private UserTask() { } // EF Core

    private UserTask(
        string name,
        Guid createByUserId,
        Guid? assignedToUserId,
        Guid? teamId,
        string? markdownText,
        DateTimeOffset? dueAt)
    {
        Id = Guid.NewGuid();
        Name = name;
        CreateByUserId = createByUserId;
        AssignedToUserId = assignedToUserId;
        TeamId = teamId;
        MarkdownText = markdownText;
        DueAt = dueAt?.ToUniversalTime();

        Status = UserTaskStatus.New;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<UserTask> Create(
        string name,
        Guid createByUserId,
        Guid? teamId = null,
        Guid? assignedToUserId = null,
        string? markdownText = null,
        DateTimeOffset? dueAt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new Error(
                "Validation",
                "Task name cannot be empty");

        if (name.Length > MaxNameLength)
            return new Error(
                "Validation",
                $"Task name cannot exceed {MaxNameLength} characters");

        if (createByUserId == Guid.Empty)
            return new Error(
                "Validation",
                "CreateByUserId cannot be empty");

        if (markdownText?.Length > MaxMarkdownLength)
            return new Error(
                "Validation",
                $"Markdown cannot exceed {MaxMarkdownLength} characters");

        return new UserTask(
            name.Trim(),
            createByUserId,
            assignedToUserId,
            teamId,
            markdownText,
            dueAt);
    }

    public Result ChangeName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return new Error(
                "Validation",
                "Task name cannot be empty");

        if (newName.Length > MaxNameLength)
            return new Error(
                "Validation",
                $"Task name cannot exceed {MaxNameLength} characters");

        newName = newName.Trim();

        if (Name == newName)
            return Result.Success();

        Name = newName;
        Touch();

        return Result.Success();
    }

    public Result ChangeMarkdownText(string? markdownText)
    {
        if (markdownText?.Length > MaxMarkdownLength)
            return new Error(
                "Validation",
                $"Markdown cannot exceed {MaxMarkdownLength} characters");

        if (MarkdownText == markdownText)
            return Result.Success();

        MarkdownText = markdownText;
        Touch();

        return Result.Success();
    }

    public Result ChangeDueAt(DateTimeOffset? dueAt)
    {
        var normalizedDueAt = dueAt?.ToUniversalTime();

        if (DueAt == normalizedDueAt)
            return Result.Success();

        DueAt = normalizedDueAt;
        Touch();

        return Result.Success();
    }

    public Result AssignTo(Guid userId)
    {
        if (userId == Guid.Empty)
            return new Error(
                "Validation",
                "UserId cannot be empty");

        if (AssignedToUserId == userId)
            return Result.Success();

        AssignedToUserId = userId;
        Touch();

        return Result.Success();
    }

    public Result ChangeStatus(UserTaskStatus newStatus)
    {
        if (!Enum.IsDefined(newStatus))
            return new Error(
                "Validation",
                "Invalid task status");

        if (Status == newStatus)
            return Result.Success();

        if (!CanTransitionTo(newStatus))
            return new Error(
                "Validation",
                $"Cannot change task status from {Status} to {newStatus}");

        Status = newStatus;
        Touch();

        return Result.Success();
    }

    public bool IsOverdue(DateTimeOffset now)
    {
        return DueAt.HasValue
            && DueAt.Value < now.ToUniversalTime()
            && Status is not UserTaskStatus.Completed
                and not UserTaskStatus.Cancelled;
    }

    private bool CanTransitionTo(UserTaskStatus nextStatus)
    {
        return Status switch
        {
            UserTaskStatus.New =>
                nextStatus is UserTaskStatus.InProgress
                    or UserTaskStatus.Completed
                    or UserTaskStatus.Cancelled,

            UserTaskStatus.InProgress =>
                nextStatus is UserTaskStatus.InReview
                    or UserTaskStatus.Blocked
                    or UserTaskStatus.Completed
                    or UserTaskStatus.Cancelled,

            UserTaskStatus.InReview =>
                nextStatus is UserTaskStatus.InProgress
                    or UserTaskStatus.Blocked
                    or UserTaskStatus.Completed
                    or UserTaskStatus.Cancelled,

            UserTaskStatus.Blocked =>
                nextStatus is UserTaskStatus.InProgress
                    or UserTaskStatus.InReview
                    or UserTaskStatus.Completed
                    or UserTaskStatus.Cancelled,

            // Повторное открытие задачи.
            UserTaskStatus.Completed =>
                nextStatus == UserTaskStatus.InProgress,

            UserTaskStatus.Cancelled =>
                nextStatus == UserTaskStatus.New,

            _ => false
        };
    }

    private void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum UserTaskStatus
{
    New,
    InProgress,
    InReview,
    Blocked,
    Completed,
    Cancelled
}