using System.ComponentModel.DataAnnotations;
using src.Domain.Entities;

/// <summary>
/// DTO для регистрации нового пользователя
/// </summary>
/// <param name="Name">Полное имя пользователя</param>
/// <param name="Email">Email адрес для входа</param>
/// <param name="Password">Пароль (минимум 8 символов, должен содержать буквы и цифры)</param>
public record CreateUserDto(
    [Required] string Name,
    [Required] [EmailAddress] string Email,
    [Required] string Password,
    bool? Visible,
    string? DisplayName);

/// <summary>
/// DTO для входа в систему
/// </summary>
/// <param name="Email">Email адрес пользователя</param>
/// <param name="Password">Пароль пользователя</param>
public record LoginUserDto(
    [Required] [EmailAddress] string Email,
    [Required] string Password);

/// <summary>
/// DTO для создания новой задачи
/// </summary>
/// <param name="Name">Название задачи</param>
/// <param name="AssignedToUserId">ID пользователя, которому назначается задача (опционально)</param>
/// <param name="TeamId">ID команды, если задача командная (опционально)</param>
public record CreateTaskDto(
    [Required] string Name,
    Guid? AssignedToUserId,
    Guid? TeamId,
    string? MarkdownText, 
    DateTimeOffset? DueAt);

/// <summary>
/// DTO для удаления задачи
/// </summary>
/// <param name="id">ID задачи для удаления</param>
public record DeleteTaskDto(
    [Required] Guid id);

/// <summary>
/// DTO для получения задачи по ID
/// </summary>
/// <param name="id">ID задачи</param>
public record GetTaskDto(
    [Required] Guid id);

/// <summary>
/// DTO для переименования задачи
/// </summary>
/// <param name="id">ID задачи</param>
/// <param name="Name">Новое название задачи</param>
public record RenameTaskDto(
    [Required] Guid id,
    [Required] string Name
);

/// <summary>
/// DTO для назначения задачи пользователю
/// </summary>
/// <param name="id">ID задачи</param>
/// <param name="AssigneeToUserId">ID пользователя, которому назначается задача</param>
public record AssignTaskDto(
    [Required] Guid id,
    [Required] Guid AssigneeToUserId
);

/// <summary>
/// DTO для удаления команды
/// </summary>
/// <param name="id">ID команды</param>
public record DeleteTeamDto(
    [Required] Guid id);

/// <summary>
/// DTO для добавления пользователя в команду
/// </summary>
/// <param name="TeamId">ID команды</param>
/// <param name="UserId">ID пользователя</param>
/// <param name="IsAdmin">Признак администратора команды</param>
public record AddUserInTeamDto(
    [Required] Guid UserId,
    [Required] bool IsAdmin);

/// <summary>
/// DTO для удаления пользователя из команды
/// </summary>
/// <param name="TeamId">ID команды</param>
/// <param name="UserId">ID пользователя</param>
public record DeleteUserFromTeamDto(
    [Required] Guid UserId
);

/// <summary>
/// DTO для получения всех задач команды
/// </summary>
/// <param name="TeamId">ID команды</param>
public record GetAllTeamTaskDto(
    [Required] Guid TeamId);

/// <summary>
/// DTO для создания новой команды
/// </summary>
/// <param name="Name">Название команды</param>
public record CreateTeamDto(
    [Required] string Name,
    JoinPolicy? policy,
    string idName
);

/// <summary>
/// DTO для удаления пользователя
/// </summary>
/// <param name="id">ID пользователя</param>
public record DeleteUserDto(
    [Required] Guid id
);

/// <summary>
/// DTO для получения информации о команде
/// </summary>
/// <param name="id">ID команды</param>
public record GetTeamDto(
    [Required] Guid id);


public class JoinRequestDto
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
}

public record UserSearchDto(
    Guid Id,
    string DisplayName
);

public record TeamSearchDto(
    Guid Id,
    string IdName);
    
public record UserEndpointSearchDto(
    string DisplayName);
    


public record TeamEndpointSearchDto (
    string IdName);
    
public record AccountDto(
    string Name,
    string DisplayName,
    string Email,
    bool Visible
);

public record ChangeDisplayNameDto(string DisplayName);

public record ChangeVisibilityDto(bool Visible);

public record ChangePasswordDto(
    [Required] string CurrentPassword,
    [Required] string NewPassword);
    
public record ChangeEmailDto(
    [Required] string NewEmail);
    
public record ChangeUserNameDto(
    [Required] string  NewUserName);
    
public record PaginationDto(
    [Required] int Skip,
    [Required]  int Take);

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Message,
    Guid? TaskId,
    Guid? TeamId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public record NotificationsUserDto(
    IReadOnlyList<NotificationDto> Notifications);
    
public record ChangeTaskStatusDto(UserTaskStatus Status);

public record ChangeTaskMarkdownDto(string? MarkdownText);

public record ChangeTaskDueAtDto(DateTimeOffset? DueAt);