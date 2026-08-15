using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO для регистрации нового пользователя
/// </summary>
/// <param name="Name">Полное имя пользователя</param>
/// <param name="Email">Email адрес для входа</param>
/// <param name="Password">Пароль (минимум 8 символов, должен содержать буквы и цифры)</param>
public record CreateUserDto(
    [Required] string Name,
    [Required] [EmailAddress] string Email,
    [Required] string Password);

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
/// <param name="assignedToUserId">ID пользователя, которому назначается задача (опционально)</param>
/// <param name="teamId">ID команды, если задача командная (опционально)</param>
public record CreateTaskDto(
    [Required] string Name,
    Guid? assignedToUserId,
    Guid? teamId);

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
    [Required] Guid TeamId,
    [Required] Guid UserId,
    bool IsAdmin);

/// <summary>
/// DTO для удаления пользователя из команды
/// </summary>
/// <param name="TeamId">ID команды</param>
/// <param name="UserId">ID пользователя</param>
public record DeleteUserFromTeamDto(
    [Required] Guid TeamId,
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
    [Required] string Name
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
    
/// <summary>
/// DTO для обновления роли пользователя в команде
/// </summary>
/// <param name="TeamId">ID команды</param>
/// <param name="UserId">ID пользователя</param>
public record UpdateTeamRoleDto(
    [Required] Guid TeamId,
    [Required] Guid UserId);