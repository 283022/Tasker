namespace src.Dto;

public sealed record TeamDto(
    Guid Id,
    string Name,
    List<TeamMemberDto> Members);

public sealed record TeamMemberDto(
    Guid UserId,
    bool IsAdmin,
    string UserName);