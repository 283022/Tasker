using src.Domain;
using src.Domain.Entities;
using src.UnitOfWork;

namespace src.Service;

public class TeamService(IUnitOfWork uow)
{
    private readonly IUnitOfWork _unitOfWork = uow;

    public async Task<Result> CreateAsync(Guid userId, CreateTeamDto createTeamDto)
    {
        if (userId == Guid.Empty)
            return Result.Failure(Errors.Validation("User ID cannot be empty.", "userId"));

        if (createTeamDto is null || string.IsNullOrWhiteSpace(createTeamDto.Name))
            return Result.Failure(Errors.Validation("Team name cannot be empty.", "name"));

        var teamResult = Team.Create(createTeamDto.Name);
        if (teamResult.IsFailure)
            return Result.Failure(teamResult.Error);

        var team = teamResult.Value;

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _unitOfWork.TeamRepository.AddTeamAsync(team);
            await _unitOfWork.SaveChangesAsync();

            var teamMemberResult = TeamMember.Create(team.Id, userId, isAdmin: true);
            if (teamMemberResult.IsFailure)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result.Failure(teamMemberResult.Error);
            }

            await _unitOfWork.TeamMemberRepository.AddTeamMemberAsync(teamMemberResult.Value);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return Result.Success();
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure(Errors.Internal("An error occurred while creating the team."));
        }
    }

    public async Task<Result> DeleteAsync(DeleteTeamDto deleteTeamDto, Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure(Errors.Validation("User ID cannot be empty.", "userId"));

        if (deleteTeamDto is null)
            return Result.Failure(Errors.Validation("Request cannot be empty."));

        if (deleteTeamDto.id == Guid.Empty)
            return Result.Failure(Errors.Validation("Team ID cannot be empty.", "teamId"));

        var team = await _unitOfWork.TeamRepository.GetTeamByIdAsync(deleteTeamDto.id);
        if (team is null)
            return Result.Failure(Errors.NotFound("Team", deleteTeamDto.id.ToString()));

        var isUserAdminOfTeam = await _unitOfWork.TeamRepository.IsUserAdminOfTeamAsync(userId, deleteTeamDto.id);
        if (!isUserAdminOfTeam)
            return Result.Failure(Errors.Forbidden("Only team administrators can delete this team."));

        await _unitOfWork.TeamRepository.DeleteTeamAsync(team);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> AddUserAsync(AddUserInTeamDto addUserInTeamDto, Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure(Errors.Validation("User ID cannot be empty.", "userId"));

        if (addUserInTeamDto is null)
            return Result.Failure(Errors.Validation("Request cannot be empty."));

        if (addUserInTeamDto.TeamId == Guid.Empty)
            return Result.Failure(Errors.Validation("Team ID cannot be empty.", "teamId"));

        if (addUserInTeamDto.UserId == Guid.Empty)
            return Result.Failure(Errors.Validation("User ID to add cannot be empty.", "userId"));

        var team = await _unitOfWork.TeamRepository.GetTeamByIdAsync(addUserInTeamDto.TeamId);
        if (team is null)
            return Result.Failure(Errors.NotFound("Team", addUserInTeamDto.TeamId.ToString()));

        var isUserAdminOfTeam = await _unitOfWork.TeamRepository.IsUserAdminOfTeamAsync(userId, team.Id);
        if (!isUserAdminOfTeam)
            return Result.Failure(Errors.Forbidden("Only team administrators can add users."));

        var isUserInTeam = await _unitOfWork.TeamRepository.IsUserInTeamAsync(addUserInTeamDto.UserId, team.Id);
        if (isUserInTeam)
            return Result.Failure(Errors.Conflict("User is already a member of this team."));

        var teamMember = TeamMember.Create(
            addUserInTeamDto.TeamId, addUserInTeamDto.UserId, addUserInTeamDto.IsAdmin);

        if (teamMember.IsFailure)
            return Result.Failure(Errors.Internal("Failed to create team membership."));

        await _unitOfWork.TeamMemberRepository.AddTeamMemberAsync(teamMember.Value);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(DeleteUserFromTeamDto deleteUserFromTeamDto, Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure(Errors.Validation("User ID cannot be empty.", "userId"));

        if (deleteUserFromTeamDto is null)
            return Result.Failure(Errors.Validation("Request cannot be empty."));

        if (deleteUserFromTeamDto.TeamId == Guid.Empty)
            return Result.Failure(Errors.Validation("Team ID cannot be empty.", "teamId"));

        if (deleteUserFromTeamDto.UserId == Guid.Empty)
            return Result.Failure(Errors.Validation("User ID to remove cannot be empty.", "userId"));

        var team = await _unitOfWork.TeamRepository.GetTeamByIdAsync(deleteUserFromTeamDto.TeamId);
        if (team is null)
            return Result.Failure(Errors.NotFound("Team", deleteUserFromTeamDto.TeamId.ToString()));

        var isUserAdminOfTeam = await _unitOfWork.TeamRepository.IsUserAdminOfTeamAsync(userId, team.Id);
        if (!isUserAdminOfTeam)
            return Result.Failure(Errors.Forbidden("Only team administrators can remove users."));

        var isUserInTeam = await _unitOfWork.TeamRepository.IsUserInTeamAsync(deleteUserFromTeamDto.UserId, team.Id);
        if (!isUserInTeam)
            return Result.Failure(Errors.Validation("User is not a member of this team.", "userId"));

        var teamMember = await _unitOfWork.TeamMemberRepository.GetByTeamAndUserAsync(
            deleteUserFromTeamDto.TeamId, deleteUserFromTeamDto.UserId);

        if (teamMember is null)
            return Result.Failure(Errors.NotFound("Team membership not found."));

        await _unitOfWork.TeamMemberRepository.DeleteTeamMemberAsync(teamMember);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<IEnumerable<TeamMember>>> GetAllTeamWhereUserMemberAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result<IEnumerable<TeamMember>>.Failure(Errors.Validation("User ID cannot be empty.", "userId"));

        var result = await _unitOfWork.TeamMemberRepository.GetByUserIdAsync(userId);
        return Result<IEnumerable<TeamMember>>.Success(result);
    }

    public async Task<Result<IEnumerable<UserTask>>> GetAllTasksTeamAsync(GetAllTeamTaskDto getAllTeamTaskDto, Guid userId)
    {
        if (userId == Guid.Empty)
            return Result<IEnumerable<UserTask>>.Failure(Errors.Validation("User ID cannot be empty.", "userId"));

        if (getAllTeamTaskDto is null)
            return Result<IEnumerable<UserTask>>.Failure(Errors.Validation("Request cannot be empty."));

        if (getAllTeamTaskDto.TeamId == Guid.Empty)
            return Result<IEnumerable<UserTask>>.Failure(Errors.Validation("Team ID cannot be empty.", "teamId"));

        var team = await _unitOfWork.TeamRepository.GetTeamByIdAsync(getAllTeamTaskDto.TeamId);
        if (team is null)
            return Result<IEnumerable<UserTask>>.Failure(Errors.NotFound("Team", getAllTeamTaskDto.TeamId.ToString()));

        var isUserInTeam = await _unitOfWork.TeamMemberRepository.IsUserInTeamAsync(getAllTeamTaskDto.TeamId, userId);
        if (!isUserInTeam)
            return Result<IEnumerable<UserTask>>.Failure(Errors.Forbidden("You must be a team member to view tasks."));

        var tasks = await _unitOfWork.UserTaskRepository.GetTasksByTeamIdAsync(getAllTeamTaskDto.TeamId);
        return Result<IEnumerable<UserTask>>.Success(tasks);
    }

    public async Task<Result<ICollection<TeamMember>>> GetAllTeamWithUsersAsync(GetTeamDto dto, Guid currentUserId)
    {
        if (dto == null)
            return Result<ICollection<TeamMember>>.Failure(Errors.Validation("Request cannot be empty."));

        if (dto.id == Guid.Empty)
            return Result<ICollection<TeamMember>>.Failure(Errors.Validation("Team ID cannot be empty.", "teamId"));

        if (currentUserId == Guid.Empty)
            return Result<ICollection<TeamMember>>.Failure(Errors.Validation("User ID cannot be empty.", "userId"));

        var team = await _unitOfWork.TeamRepository.GetTeamByIdAsync(dto.id);
        if (team is null)
            return Result<ICollection<TeamMember>>.Failure(Errors.NotFound("Team", dto.id.ToString()));

        var isUserInTeam = await _unitOfWork.TeamMemberRepository.IsUserInTeamAsync(dto.id, currentUserId);
        if (!isUserInTeam)
            return Result<ICollection<TeamMember>>.Failure(Errors.Forbidden("You must be a team member to view members."));

        var members = await _unitOfWork.TeamRepository.GetTeamWithMembersAsync(dto.id);
        if (members is null || members.Members is null)
            return Result<ICollection<TeamMember>>.Failure(Errors.NotFound("Team members not found."));

        return Result<ICollection<TeamMember>>.Success(members.Members);
    }

    public async Task<Result> UpdateMemberRoleAsync(UpdateTeamRoleDto dto, Guid userId)
    {
        if (dto is null)
            return Result.Failure(Errors.Validation("Request cannot be empty."));

        if (dto.TeamId == Guid.Empty)
            return Result.Failure(Errors.Validation("Team ID cannot be empty.", "teamId"));

        if (dto.UserId == Guid.Empty)
            return Result.Failure(Errors.Validation("User ID to update cannot be empty.", "userId"));

        if (userId == Guid.Empty)
            return Result.Failure(Errors.Validation("Current user ID cannot be empty.", "userId"));

        var team = await _unitOfWork.TeamRepository.GetTeamByIdAsync(dto.TeamId);
        if (team is null)
            return Result.Failure(Errors.NotFound("Team", dto.TeamId.ToString()));

        var isAdmin = await _unitOfWork.TeamMemberRepository.IsUserAdminAsync(dto.TeamId, userId);
        if (!isAdmin)
            return Result.Failure(Errors.Forbidden("Only team administrators can update roles."));

        var teamMember = await _unitOfWork.TeamMemberRepository.GetByTeamAndUserAsync(dto.TeamId, dto.UserId);
        if (teamMember is null)
            return Result.Failure(Errors.NotFound("Team member not found."));

        if (teamMember.IsAdmin)
            return Result.Failure(Errors.Conflict("User is already a team administrator."));

        teamMember.ChangeRole(true);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }
}