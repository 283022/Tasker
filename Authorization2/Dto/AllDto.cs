namespace Authorization2;

public record CreateUserDto(string Name, string Email, string Password);
public record CreateTaskDto(Guid userid, string Name);
public record DeleteTaskDto(Guid id);
public record GetTaskDto(Guid id);

public record LoginUserDto(string Email, string Password);