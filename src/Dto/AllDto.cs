using System.ComponentModel.DataAnnotations;

namespace src;

public record CreateUserDto(
    [Required] string Name,
    [Required] [EmailAddress] string Email,
    [Required] string Password);

public record CreateTaskDto(
    [Required] string Name,
    Guid? assignedToUserId,
    Guid? teamId);

public record DeleteTaskDto(
    [Required] Guid id);

public record GetTaskDto(
    [Required] Guid id);

public record RenameTaskDto(
    [Required] Guid id,
    [Required] string Name
);

public record AssignTaskDto(
    [Required] Guid id,
    [Required] Guid AssigneeToUserId
);

public record DeleteTeamDto(
    [Required] Guid id);

public record AddUserInTeamDto(
    [Required] Guid TeamId,
    [Required] Guid UserId,
    bool IsAdmin);

public record DeleteUserFromTeamDto(
    [Required] Guid TeamId,
    [Required] Guid UserId
);

public record GetAllTeamTaskDto(
    [Required] Guid TeamId);

public record LoginUserDto(
    [Required] [EmailAddress] string Email,
    [Required] string Password);

public record CreateTeamDto(
    [Required] string Name
);

public record DeleteUserDto(
    [Required] Guid id
);

public record GetTeamDto(
    [Required] Guid id);
    
    public record UpdateTeamRoleDto(
        [Required] Guid TeamId,
        [Required] Guid UserId);