using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using src.Domain.Entities;

namespace src.Dto;

public sealed record TeamDto(
    Guid Id,
    string Name,
    string IdName,
    string Policy,
    List<TeamMemberDto> Members);

public sealed record TeamMemberDto(
    Guid UserId,
    bool IsAdmin,
    string UserName);