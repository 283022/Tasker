using System.ComponentModel.DataAnnotations;

namespace Authorization2;

public record CreateUserDto(
    [Required] string Name, 
    [Required] [EmailAddress] string Email, 
    [Required] string Password);
public record CreateTaskDto(
    [Required] Guid userid, 
    [Required] string Name);
public record DeleteTaskDto(
    [Required] Guid id);
public record GetTaskDto(
    [Required] Guid id);

public record LoginUserDto(
    [Required] [EmailAddress] string Email, 
    [Required] string Password);