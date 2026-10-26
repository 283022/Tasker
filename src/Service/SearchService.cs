using src.Domain;
using src.UnitOfWork;

namespace src.Service;

public class SearchService(IUnitOfWork uow)
{
    private readonly IUnitOfWork _unitOfWork = uow;
    
    public async Task<Result<List<UserSearchDto>>> SearchUsersAsync(
        UserEndpointSearchDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(dto.DisplayName))
            return Result<List<UserSearchDto>>.Failure(Errors.Validation($"UserID cannot be empty."));
        
        var users = await _unitOfWork.UserRepository.SearchByDisplayNameWithPagginationAsync(
            dto.DisplayName,
            cancellationToken);

        if (users.Count == 0)
            return Result<List<UserSearchDto>>.Failure(Errors.NotFound($"User not found."));
        
        return users
            .Select(u => new UserSearchDto(
                u.Id,
                u.DisplayName))
            .ToList();
    }
    
    public async Task<Result<List<TeamSearchDto>>> SearchTeamAsync(
        TeamEndpointSearchDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(dto.IdName))
            return Result<List<TeamSearchDto>>.Failure(Errors.Validation($"TeamID cannot be empty."));
        
        var teams = await _unitOfWork.TeamRepository.SearchByIdNameAsync(
            dto.IdName,
            cancellationToken);

        if (teams.Count == 0)
            return Result<List<TeamSearchDto>>.Failure(Errors.NotFound($"User not found."));
        
        return teams
            .Select(u => new TeamSearchDto(
                u.Id,
                u.IdName))
            .ToList();
    }
    
    
    
}