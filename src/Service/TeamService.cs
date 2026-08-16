using src.Domain;
using src.Domain.Entities;
using src.UnitOfWork;

namespace src.Service;

public class TeamService(IUnitOfWork unitOfWork, ILogger<TeamService> logger)
{
    public async Task<Result> CreateAsync(Guid userId, CreateTeamDto createTeamDto)
    {
        logger.LogInformation("Creating new team '{TeamName}' by user {UserId}",
            createTeamDto?.Name, userId);

        if (userId == Guid.Empty)
        {
            logger.LogWarning("Team creation failed: user ID is empty");
            return Result.Failure(Errors.Validation("User ID cannot be empty.", "userId"));
        }

        if (createTeamDto is null || string.IsNullOrWhiteSpace(createTeamDto.Name))
        {
            logger.LogWarning("Team creation failed: team name is empty");
            return Result.Failure(Errors.Validation("Team name cannot be empty.", "name"));
        }

        var teamResult = Team.Create(createTeamDto.Name);
        if (teamResult.IsFailure)
        {
            logger.LogWarning("Team creation failed: invalid team data. Error: {Error}", teamResult.Error.Message);
            return Result.Failure(teamResult.Error);
        }

        var team = teamResult.Value;

        await unitOfWork.BeginTransactionAsync();

        try
        {
            await unitOfWork.TeamRepository.AddTeamAsync(team);
            await unitOfWork.SaveChangesAsync();

            var teamMemberResult = TeamMember.Create(team.Id, userId, isAdmin: true);
            if (teamMemberResult.IsFailure)
            {
                logger.LogWarning("Team creation failed: failed to create team member. Error: {Error}",
                    teamMemberResult.Error.Message);
                await unitOfWork.RollbackTransactionAsync();
                return Result.Failure(teamMemberResult.Error);
            }

            await unitOfWork.TeamMemberRepository.AddTeamMemberAsync(teamMemberResult.Value);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            logger.LogInformation("Team {TeamId} '{TeamName}' created successfully by user {UserId}",
                team.Id, team.Name, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating team '{TeamName}' by user {UserId}", createTeamDto.Name, userId);
            await unitOfWork.RollbackTransactionAsync();
            return Result.Failure(Errors.Internal("An error occurred while creating the team."));
        }
    }

    public async Task<Result> DeleteAsync(DeleteTeamDto deleteTeamDto, Guid userId)
    {
        logger.LogInformation("Delete team request for team {TeamId} by user {UserId}",
            deleteTeamDto?.id, userId);

        if (userId == Guid.Empty)
        {
            logger.LogWarning("Delete team failed: user ID is empty");
            return Result.Failure(Errors.Validation("User ID cannot be empty.", "userId"));
        }

        if (deleteTeamDto is null)
        {
            logger.LogWarning("Delete team failed: request is null");
            return Result.Failure(Errors.Validation("Request cannot be empty."));
        }

        if (deleteTeamDto.id == Guid.Empty)
        {
            logger.LogWarning("Delete team failed: team ID is empty");
            return Result.Failure(Errors.Validation("Team ID cannot be empty.", "teamId"));
        }

        var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(deleteTeamDto.id);
        if (team is null)
        {
            logger.LogWarning("Delete team failed: team {TeamId} not found", deleteTeamDto.id);
            return Result.Failure(Errors.NotFound("Team", deleteTeamDto.id.ToString()));
        }

        var isUserAdminOfTeam = await unitOfWork.TeamRepository.IsUserAdminOfTeamAsync(userId, deleteTeamDto.id);
        if (!isUserAdminOfTeam)
        {
            logger.LogWarning("Delete team failed: user {UserId} is not admin of team {TeamId}",
                userId, deleteTeamDto.id);
            return Result.Failure(Errors.Forbidden("Only team administrators can delete this team."));
        }

        await unitOfWork.TeamRepository.DeleteTeamAsync(team);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Team {TeamId} '{TeamName}' deleted successfully by user {UserId}",
            team.Id, team.Name, userId);

        return Result.Success();
    }

    public async Task<Result> AddUserAsync(AddUserInTeamDto addUserInTeamDto, Guid userId)
    {
        logger.LogInformation("Adding user {TargetUserId} to team {TeamId} by user {UserId}",
            addUserInTeamDto?.UserId, addUserInTeamDto?.TeamId, userId);

        if (userId == Guid.Empty)
        {
            logger.LogWarning("Add user to team failed: user ID is empty");
            return Result.Failure(Errors.Validation("User ID cannot be empty.", "userId"));
        }

        if (addUserInTeamDto is null)
        {
            logger.LogWarning("Add user to team failed: request is null");
            return Result.Failure(Errors.Validation("Request cannot be empty."));
        }

        if (addUserInTeamDto.TeamId == Guid.Empty)
        {
            logger.LogWarning("Add user to team failed: team ID is empty");
            return Result.Failure(Errors.Validation("Team ID cannot be empty.", "teamId"));
        }

        if (addUserInTeamDto.UserId == Guid.Empty)
        {
            logger.LogWarning("Add user to team failed: target user ID is empty");
            return Result.Failure(Errors.Validation("User ID to add cannot be empty.", "userId"));
        }

        var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(addUserInTeamDto.TeamId);
        if (team is null)
        {
            logger.LogWarning("Add user to team failed: team {TeamId} not found", addUserInTeamDto.TeamId);
            return Result.Failure(Errors.NotFound("Team", addUserInTeamDto.TeamId.ToString()));
        }

        var isUserAdminOfTeam = await unitOfWork.TeamRepository.IsUserAdminOfTeamAsync(userId, team.Id);
        if (!isUserAdminOfTeam)
        {
            logger.LogWarning("Add user to team failed: user {UserId} is not admin of team {TeamId}",
                userId, team.Id);
            return Result.Failure(Errors.Forbidden("Only team administrators can add users."));
        }

        var isUserInTeam = await unitOfWork.TeamRepository.IsUserInTeamAsync(addUserInTeamDto.UserId, team.Id);
        if (isUserInTeam)
        {
            logger.LogWarning("Add user to team failed: user {TargetUserId} is already in team {TeamId}",
                addUserInTeamDto.UserId, team.Id);
            return Result.Failure(Errors.Conflict("User is already a member of this team."));
        }

        var teamMember = TeamMember.Create(
            addUserInTeamDto.TeamId,
            addUserInTeamDto.UserId,
            addUserInTeamDto.IsAdmin);

        if (teamMember.IsFailure)
        {
            logger.LogWarning("Add user to team failed: failed to create team membership. Error: {Error}",
                teamMember.Error.Message);
            return Result.Failure(Errors.Internal("Failed to create team membership."));
        }

        await unitOfWork.TeamMemberRepository.AddTeamMemberAsync(teamMember.Value);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("User {TargetUserId} added to team {TeamId} by user {UserId}, isAdmin: {IsAdmin}",
            addUserInTeamDto.UserId, team.Id, userId, addUserInTeamDto.IsAdmin);

        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(DeleteUserFromTeamDto deleteUserFromTeamDto, Guid userId)
    {
        logger.LogInformation("Removing user {TargetUserId} from team {TeamId} by user {UserId}",
            deleteUserFromTeamDto?.UserId, deleteUserFromTeamDto?.TeamId, userId);

        if (userId == Guid.Empty)
        {
            logger.LogWarning("Remove user from team failed: user ID is empty");
            return Result.Failure(Errors.Validation("User ID cannot be empty.", "userId"));
        }

        if (deleteUserFromTeamDto is null)
        {
            logger.LogWarning("Remove user from team failed: request is null");
            return Result.Failure(Errors.Validation("Request cannot be empty."));
        }

        if (deleteUserFromTeamDto.TeamId == Guid.Empty)
        {
            logger.LogWarning("Remove user from team failed: team ID is empty");
            return Result.Failure(Errors.Validation("Team ID cannot be empty.", "teamId"));
        }

        if (deleteUserFromTeamDto.UserId == Guid.Empty)
        {
            logger.LogWarning("Remove user from team failed: target user ID is empty");
            return Result.Failure(Errors.Validation("User ID to remove cannot be empty.", "userId"));
        }

        var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(deleteUserFromTeamDto.TeamId);
        if (team is null)
        {
            logger.LogWarning("Remove user from team failed: team {TeamId} not found", deleteUserFromTeamDto.TeamId);
            return Result.Failure(Errors.NotFound("Team", deleteUserFromTeamDto.TeamId.ToString()));
        }

        var isUserAdminOfTeam = await unitOfWork.TeamRepository.IsUserAdminOfTeamAsync(userId, team.Id);
        if (!isUserAdminOfTeam)
        {
            logger.LogWarning("Remove user from team failed: user {UserId} is not admin of team {TeamId}",
                userId, team.Id);
            return Result.Failure(Errors.Forbidden("Only team administrators can remove users."));
        }

        var isUserInTeam = await unitOfWork.TeamRepository.IsUserInTeamAsync(
            deleteUserFromTeamDto.UserId, team.Id);
        if (!isUserInTeam)
        {
            logger.LogWarning("Remove user from team failed: user {TargetUserId} is not in team {TeamId}",
                deleteUserFromTeamDto.UserId, team.Id);
            return Result.Failure(Errors.Validation("User is not a member of this team.", "userId"));
        }

        var teamMember = await unitOfWork.TeamMemberRepository.GetByTeamAndUserAsync(
            deleteUserFromTeamDto.TeamId, deleteUserFromTeamDto.UserId);

        if (teamMember is null)
        {
            logger.LogWarning(
                "Remove user from team failed: team membership not found for user {TargetUserId} in team {TeamId}",
                deleteUserFromTeamDto.UserId, team.Id);
            return Result.Failure(Errors.NotFound("Team membership not found."));
        }

        await unitOfWork.TeamMemberRepository.DeleteTeamMemberAsync(teamMember);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("User {TargetUserId} removed from team {TeamId} by user {UserId}",
            deleteUserFromTeamDto.UserId, team.Id, userId);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<TeamMember>>> GetAllTeamWhereUserMemberAsync(Guid userId)
    {
        logger.LogDebug("Fetching all teams for user {UserId}", userId);

        if (userId == Guid.Empty)
        {
            logger.LogWarning("Get teams for user failed: user ID is empty");
            return Result<IEnumerable<TeamMember>>.Failure(Errors.Validation("User ID cannot be empty.", "userId"));
        }

        var result = await unitOfWork.TeamMemberRepository.GetByUserIdAsync(userId);

        logger.LogDebug("Found {Count} teams for user {UserId}", result.Count(), userId);
        return Result<IEnumerable<TeamMember>>.Success(result);
    }

    public async Task<Result<IEnumerable<UserTask>>> GetAllTasksTeamAsync(GetAllTeamTaskDto getAllTeamTaskDto,
        Guid userId)
    {
        logger.LogDebug("Fetching tasks for team {TeamId} by user {UserId}",
            getAllTeamTaskDto?.TeamId, userId);

        if (userId == Guid.Empty)
        {
            logger.LogWarning("Get team tasks failed: user ID is empty");
            return Result<IEnumerable<UserTask>>.Failure(Errors.Validation("User ID cannot be empty.", "userId"));
        }

        if (getAllTeamTaskDto is null)
        {
            logger.LogWarning("Get team tasks failed: request is null");
            return Result<IEnumerable<UserTask>>.Failure(Errors.Validation("Request cannot be empty."));
        }

        if (getAllTeamTaskDto.TeamId == Guid.Empty)
        {
            logger.LogWarning("Get team tasks failed: team ID is empty");
            return Result<IEnumerable<UserTask>>.Failure(Errors.Validation("Team ID cannot be empty.", "teamId"));
        }

        var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(getAllTeamTaskDto.TeamId);
        if (team is null)
        {
            logger.LogWarning("Get team tasks failed: team {TeamId} not found", getAllTeamTaskDto.TeamId);
            return Result<IEnumerable<UserTask>>.Failure(Errors.NotFound("Team", getAllTeamTaskDto.TeamId.ToString()));
        }

        var isUserInTeam = await unitOfWork.TeamMemberRepository.IsUserInTeamAsync(
            getAllTeamTaskDto.TeamId, userId);
        if (!isUserInTeam)
        {
            logger.LogWarning("Get team tasks failed: user {UserId} is not a member of team {TeamId}",
                userId, getAllTeamTaskDto.TeamId);
            return Result<IEnumerable<UserTask>>.Failure(Errors.Forbidden("You must be a team member to view tasks."));
        }

        var tasks = await unitOfWork.UserTaskRepository.GetTasksByTeamIdAsync(getAllTeamTaskDto.TeamId);

        logger.LogDebug("Found {Count} tasks for team {TeamId}", tasks.Count(), getAllTeamTaskDto.TeamId);
        return Result<IEnumerable<UserTask>>.Success(tasks);
    }

    public async Task<Result<ICollection<TeamMember>>> GetAllTeamWithUsersAsync(GetTeamDto dto, Guid currentUserId)
    {
        logger.LogDebug("Fetching team {TeamId} with users by user {UserId}",
            dto?.id, currentUserId);

        if (dto == null)
        {
            logger.LogWarning("Get team with users failed: request is null");
            return Result<ICollection<TeamMember>>.Failure(Errors.Validation("Request cannot be empty."));
        }

        if (dto.id == Guid.Empty)
        {
            logger.LogWarning("Get team with users failed: team ID is empty");
            return Result<ICollection<TeamMember>>.Failure(Errors.Validation("Team ID cannot be empty.", "teamId"));
        }

        if (currentUserId == Guid.Empty)
        {
            logger.LogWarning("Get team with users failed: user ID is empty");
            return Result<ICollection<TeamMember>>.Failure(Errors.Validation("User ID cannot be empty.", "userId"));
        }

        var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(dto.id);
        if (team is null)
        {
            logger.LogWarning("Get team with users failed: team {TeamId} not found", dto.id);
            return Result<ICollection<TeamMember>>.Failure(Errors.NotFound("Team", dto.id.ToString()));
        }

        var isUserInTeam = await unitOfWork.TeamMemberRepository.IsUserInTeamAsync(dto.id, currentUserId);
        if (!isUserInTeam)
        {
            logger.LogWarning("Get team with users failed: user {UserId} is not a member of team {TeamId}",
                currentUserId, dto.id);
            return Result<ICollection<TeamMember>>.Failure(
                Errors.Forbidden("You must be a team member to view members."));
        }

        var members = await unitOfWork.TeamRepository.GetTeamWithMembersAsync(dto.id);
        if (members is null || members.Members is null)
        {
            logger.LogWarning("Get team with users failed: no members found for team {TeamId}", dto.id);
            return Result<ICollection<TeamMember>>.Failure(Errors.NotFound("Team members not found."));
        }

        logger.LogDebug("Found {Count} members in team {TeamId}", members.Members.Count, dto.id);
        return Result<ICollection<TeamMember>>.Success(members.Members);
    }

    public async Task<Result> UpdateMemberRoleAsync(UpdateTeamRoleDto dto, Guid userId)
    {
        logger.LogInformation("Update member role request for user {TargetUserId} in team {TeamId} by user {UserId}",
            dto?.UserId, dto?.TeamId, userId);

        if (dto is null)
        {
            logger.LogWarning("Update member role failed: request is null");
            return Result.Failure(Errors.Validation("Request cannot be empty."));
        }

        if (dto.TeamId == Guid.Empty)
        {
            logger.LogWarning("Update member role failed: team ID is empty");
            return Result.Failure(Errors.Validation("Team ID cannot be empty.", "teamId"));
        }

        if (dto.UserId == Guid.Empty)
        {
            logger.LogWarning("Update member role failed: target user ID is empty");
            return Result.Failure(Errors.Validation("User ID to update cannot be empty.", "userId"));
        }

        if (userId == Guid.Empty)
        {
            logger.LogWarning("Update member role failed: current user ID is empty");
            return Result.Failure(Errors.Validation("Current user ID cannot be empty.", "userId"));
        }

        var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(dto.TeamId);
        if (team is null)
        {
            logger.LogWarning("Update member role failed: team {TeamId} not found", dto.TeamId);
            return Result.Failure(Errors.NotFound("Team", dto.TeamId.ToString()));
        }

        var isAdmin = await unitOfWork.TeamMemberRepository.IsUserAdminAsync(dto.TeamId, userId);
        if (!isAdmin)
        {
            logger.LogWarning("Update member role failed: user {UserId} is not admin of team {TeamId}",
                userId, dto.TeamId);
            return Result.Failure(Errors.Forbidden("Only team administrators can update roles."));
        }

        var teamMember = await unitOfWork.TeamMemberRepository.GetByTeamAndUserAsync(dto.TeamId, dto.UserId);
        if (teamMember is null)
        {
            logger.LogWarning(
                "Update member role failed: team member not found for user {TargetUserId} in team {TeamId}",
                dto.UserId, dto.TeamId);
            return Result.Failure(Errors.NotFound("Team member not found."));
        }

        if (teamMember.IsAdmin)
        {
            logger.LogWarning("Update member role failed: user {TargetUserId} is already admin of team {TeamId}",
                dto.UserId, dto.TeamId);
            return Result.Failure(Errors.Conflict("User is already a team administrator."));
        }

        teamMember.ChangeRole(true);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("User {TargetUserId} promoted to admin in team {TeamId} by user {UserId}",
            dto.UserId, team.Id, userId);

        return Result.Success();
    }
}