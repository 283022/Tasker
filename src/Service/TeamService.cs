using src.Domain;
using src.Domain.Entities;
using src.Dto;
using src.UnitOfWork;

namespace src.Service;

public class TeamService(IUnitOfWork unitOfWork, ILogger<TeamService> logger, NotificationService notificationService)
{
    public async Task<Result> CreateAsync(Guid userId, CreateTeamDto createTeamDto)
    {
        logger.LogInformation(
            "Creating new team '{TeamName}' by user {UserId}",
            createTeamDto?.Name, userId);

        var userValidation = ValidateUserId(userId);
        if (userValidation.IsFailure)
        {
            logger.LogWarning("Team creation failed: user ID is empty");
            return userValidation;
        }

        if (createTeamDto is null || string.IsNullOrWhiteSpace(createTeamDto.Name) || string.IsNullOrWhiteSpace(createTeamDto.idName))
        {
            logger.LogWarning("Team creation failed: team name is empty");
            return Result.Failure(
                Errors.Validation("Team name cannot be empty.", "name"));
        }
        
        var indexResult = await unitOfWork.TeamRepository.GetByIdNameAsync(createTeamDto.idName);
        
        if (indexResult is not null)
            return Result.Failure(Errors.Conflict("Team with this Id name already exists."));
        
        var teamResult = Team.Create(createTeamDto.Name, createTeamDto.policy, createTeamDto.idName);
        if (teamResult.IsFailure)
        {
            logger.LogWarning(
                "Team creation failed: invalid team data. Error: {Error}",
                teamResult.Error.Message);

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
                logger.LogWarning(
                    "Team creation failed: failed to create team member. Error: {Error}",
                    teamMemberResult.Error.Message);

                await unitOfWork.RollbackTransactionAsync();
                return Result.Failure(teamMemberResult.Error);
            }

            await unitOfWork.TeamMemberRepository.AddTeamMemberAsync(teamMemberResult.Value);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            logger.LogInformation(
                "Team {TeamId} '{TeamName}' created successfully by user {UserId}",
                team.Id, team.Name, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error creating team '{TeamName}' by user {UserId}",
                createTeamDto.Name, userId);

            await unitOfWork.RollbackTransactionAsync();

            return Result.Failure(
                Errors.Internal("An error occurred while creating the team."));
        }
    }

    public async Task<Result> DeleteAsync(DeleteTeamDto deleteTeamDto, Guid userId)
    {
        logger.LogInformation(
            "Delete team request for team {TeamId} by user {UserId}",
            deleteTeamDto?.id, userId);

        var userValidation = ValidateUserId(userId);
        if (userValidation.IsFailure)
        {
            logger.LogWarning("Delete team failed: user ID is empty");
            return userValidation;
        }

        if (deleteTeamDto is null)
        {
            logger.LogWarning("Delete team failed: request is null");
            return Result.Failure(Errors.Validation("Request cannot be empty."));
        }

        var teamIdValidation = ValidateTeamId(deleteTeamDto.id);
        if (teamIdValidation.IsFailure)
        {
            logger.LogWarning("Delete team failed: team ID is empty");
            return teamIdValidation;
        }

        var team = await unitOfWork.TeamRepository
            .GetTeamWithMembersAsync(deleteTeamDto.id);
        if (team is null)
        {
            logger.LogWarning(
                "Delete team failed: team {TeamId} not found",
                deleteTeamDto.id);

            return Result.Failure(
                Errors.NotFound("Team", deleteTeamDto.id.ToString()));
        }

        var adminResult = await EnsureTeamAdminAsync(
            userId, team.Id,
            "Delete team failed",
            "Only team administrators can delete this team.");

        if (adminResult.IsFailure)
            return adminResult;

        foreach (var member in team.Members)
        {
            // Не отправляем уведомление пользователю,
            // который сам удаляет команду.
            if (member.UserId == userId)
                continue;

            var notificationResult =
                await notificationService.NotifyTeamDeletedAsync(
                    member.UserId,
                    team.Id,
                    team.Name);

            if (notificationResult.IsFailure)
                return Result.Failure(notificationResult.Error);
        }

        await unitOfWork.TeamRepository.DeleteTeamAsync(team);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation(
            "Team {TeamId} '{TeamName}' deleted successfully by user {UserId}",
            team.Id, team.Name, userId);

        return Result.Success();
    }

    public async Task<Result> AddUserAsync(
        AddUserInTeamDto addUserInTeamDto,
        Guid teamId,
        Guid userId)
    {
        logger.LogInformation(
            "Adding user {TargetUserId} to team {TeamId} by user {UserId}",
            addUserInTeamDto?.UserId, teamId, userId);

        var userValidation = ValidateUserId(userId);
        if (userValidation.IsFailure)
        {
            logger.LogWarning("Add user to team failed: user ID is empty");
            return userValidation;
        }

        if (addUserInTeamDto is null)
        {
            logger.LogWarning("Add user to team failed: request is null");
            return Result.Failure(Errors.Validation("Request cannot be empty."));
        }

        var teamIdValidation = ValidateTeamId(teamId);
        if (teamIdValidation.IsFailure)
        {
            logger.LogWarning("Add user to team failed: team ID is empty");
            return teamIdValidation;
        }

        var targetUserValidation = ValidateTargetUserId(
            addUserInTeamDto.UserId,
            "User ID to add cannot be empty.");

        if (targetUserValidation.IsFailure)
        {
            logger.LogWarning("Add user to team failed: target user ID is empty");
            return targetUserValidation;
        }

        var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(teamId);
        if (team is null)
        {
            logger.LogWarning(
                "Add user to team failed: team {TeamId} not found",
                teamId);

            return Result.Failure(Errors.NotFound("Team", teamId.ToString()));
        }

        var adminResult = await EnsureTeamAdminAsync(
            userId, team.Id,
            "Add user to team failed",
            "Only team administrators can add users.");

        if (adminResult.IsFailure)
            return adminResult;

        var isUserInTeam = await unitOfWork.TeamRepository
            .IsUserInTeamAsync(addUserInTeamDto.UserId, team.Id);

        if (isUserInTeam)
        {
            logger.LogWarning(
                "Add user to team failed: user {TargetUserId} is already in team {TeamId}",
                addUserInTeamDto.UserId, team.Id);

            return Result.Failure(
                Errors.Conflict("User is already a member of this team."));
        }

        var isUserExist = await unitOfWork.UserRepository
            .GetUserByIdAsync(addUserInTeamDto.UserId);

        if (isUserExist is null)
        {
            logger.LogWarning("User isn't exist {Id}", addUserInTeamDto.UserId);
            return Result.Failure(Errors.NotFound("User isn't exist"));
        }

        var teamMember = TeamMember.Create(
            teamId,
            addUserInTeamDto.UserId,
            addUserInTeamDto.IsAdmin);

        if (teamMember.IsFailure)
        {
            logger.LogWarning(
                "Add user to team failed: failed to create team membership. Error: {Error}",
                teamMember.Error.Message);

            return Result.Failure(
                Errors.Internal("Failed to create team membership."));
        }

        await unitOfWork.TeamMemberRepository
            .AddTeamMemberAsync(teamMember.Value);

        var notificationResult =
            await notificationService.NotifyUserAddedToGroupAsync(
                addUserInTeamDto.UserId,
                team.Id,
                team.Name);

        if (notificationResult.IsFailure)
            return Result.Failure(notificationResult.Error);

        await unitOfWork.SaveChangesAsync();
        
        logger.LogInformation(
            "User {TargetUserId} added to team {TeamId} by user {UserId}, isAdmin: {IsAdmin}",
            addUserInTeamDto.UserId, team.Id, userId, addUserInTeamDto.IsAdmin);

        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(
        DeleteUserFromTeamDto deleteUserFromTeamDto,
        Guid teamId,
        Guid userId)
    {
        logger.LogInformation(
            "Removing user {TargetUserId} from team {TeamId} by user {UserId}",
            deleteUserFromTeamDto?.UserId, teamId, userId);

        var userValidation = ValidateUserId(userId);
        if (userValidation.IsFailure)
        {
            logger.LogWarning("Remove user from team failed: user ID is empty");
            return userValidation;
        }

        if (deleteUserFromTeamDto is null)
        {
            logger.LogWarning("Remove user from team failed: request is null");
            return Result.Failure(Errors.Validation("Request cannot be empty."));
        }

        var teamIdValidation = ValidateTeamId(teamId);
        if (teamIdValidation.IsFailure)
        {
            logger.LogWarning("Remove user from team failed: team ID is empty");
            return teamIdValidation;
        }

        var targetUserValidation = ValidateTargetUserId(
            deleteUserFromTeamDto.UserId,
            "User ID to remove cannot be empty.");

        if (targetUserValidation.IsFailure)
        {
            logger.LogWarning("Remove user from team failed: target user ID is empty");
            return targetUserValidation;
        }

        var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(teamId);
        if (team is null)
        {
            logger.LogWarning(
                "Remove user from team failed: team {TeamId} not found",
                teamId);

            return Result.Failure(Errors.NotFound("Team", teamId.ToString()));
        }

        var adminResult = await EnsureTeamAdminAsync(
            userId, team.Id,
            "Remove user from team failed",
            "Only team administrators can remove users.");

        if (adminResult.IsFailure)
            return adminResult;

        var isUserInTeam = await unitOfWork.TeamRepository
            .IsUserInTeamAsync(deleteUserFromTeamDto.UserId, team.Id);

        if (!isUserInTeam)
        {
            logger.LogWarning(
                "Remove user from team failed: user {TargetUserId} is not in team {TeamId}",
                deleteUserFromTeamDto.UserId, team.Id);

            return Result.Failure(
                Errors.Validation("User is not a member of this team.", "userId"));
        }

        var teamMember = await unitOfWork.TeamMemberRepository
            .GetByTeamAndUserAsync(teamId, deleteUserFromTeamDto.UserId);

        if (teamMember is null)
        {
            logger.LogWarning(
                "Remove user from team failed: team membership not found for user {TargetUserId} in team {TeamId}",
                deleteUserFromTeamDto.UserId, team.Id);

            return Result.Failure(
                Errors.NotFound("Team membership not found."));
        }

        var notificationResult =
            await notificationService.NotifyUserRemovedFromGroupAsync(
                deleteUserFromTeamDto.UserId,
                team.Id,
                team.Name);

        if (notificationResult.IsFailure)
            return Result.Failure(notificationResult.Error);

        await unitOfWork.TeamMemberRepository
            .DeleteTeamMemberAsync(teamMember);

        await unitOfWork.SaveChangesAsync();

        logger.LogInformation(
            "User {TargetUserId} removed from team {TeamId} by user {UserId}",
            deleteUserFromTeamDto.UserId,
            team.Id,
            userId);

        
        logger.LogInformation(
            "User {TargetUserId} removed from team {TeamId} by user {UserId}",
            deleteUserFromTeamDto.UserId, team.Id, userId);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<TeamDto>>> GetAllTeamWhereUserMemberAsync(Guid userId)
    {
        logger.LogDebug("Fetching all teams for user {UserId}", userId);

        var userValidation = ValidateUserId(userId);
        if (userValidation.IsFailure)
        {
            logger.LogWarning("Get teams for user failed: user ID is empty");
            return Result<IEnumerable<TeamDto>>.Failure(userValidation.Error);
        }

        var result = await unitOfWork.TeamRepository.GetTeamsByUserIdAsync(userId);

        logger.LogDebug("Found {Count} teams for user {UserId}", result.Count(), userId);

        return Result<IEnumerable<TeamDto>>.Success(result);
    }

    public async Task<Result<IEnumerable<UserTask>>> GetAllTasksTeamAsync(
        GetAllTeamTaskDto getAllTeamTaskDto,
        Guid userId)
    {
        logger.LogDebug(
            "Fetching tasks for team {TeamId} by user {UserId}",
            getAllTeamTaskDto?.TeamId, userId);

        var userValidation = ValidateUserId(userId);
        if (userValidation.IsFailure)
        {
            logger.LogWarning("Get team tasks failed: user ID is empty");
            return Result<IEnumerable<UserTask>>.Failure(userValidation.Error);
        }

        if (getAllTeamTaskDto is null)
        {
            logger.LogWarning("Get team tasks failed: request is null");
            return Result<IEnumerable<UserTask>>.Failure(
                Errors.Validation("Request cannot be empty."));
        }

        var teamIdValidation = ValidateTeamId(getAllTeamTaskDto.TeamId);
        if (teamIdValidation.IsFailure)
        {
            logger.LogWarning("Get team tasks failed: team ID is empty");
            return Result<IEnumerable<UserTask>>.Failure(teamIdValidation.Error);
        }

        var team = await unitOfWork.TeamRepository
            .GetTeamByIdAsync(getAllTeamTaskDto.TeamId);

        if (team is null)
        {
            logger.LogWarning(
                "Get team tasks failed: team {TeamId} not found",
                getAllTeamTaskDto.TeamId);

            return Result<IEnumerable<UserTask>>.Failure(
                Errors.NotFound("Team", getAllTeamTaskDto.TeamId.ToString()));
        }

        var isUserInTeam = await unitOfWork.TeamMemberRepository
            .IsUserInTeamAsync(getAllTeamTaskDto.TeamId, userId);

        if (!isUserInTeam)
        {
            logger.LogWarning(
                "Get team tasks failed: user {UserId} is not a member of team {TeamId}",
                userId, getAllTeamTaskDto.TeamId);

            return Result<IEnumerable<UserTask>>.Failure(
                Errors.Forbidden("You must be a team member to view tasks."));
        }

        var tasks = await unitOfWork.UserTaskRepository
            .GetTasksByTeamIdAsync(getAllTeamTaskDto.TeamId);

        logger.LogDebug(
            "Found {Count} tasks for team {TeamId}",
            tasks.Count(), getAllTeamTaskDto.TeamId);

        return Result<IEnumerable<UserTask>>.Success(tasks);
    }

    public async Task<Result<ICollection<TeamMember>>> GetAllTeamWithUsersAsync(
        GetTeamDto dto,
        Guid currentUserId)
    {
        logger.LogDebug(
            "Fetching team {TeamId} with users by user {UserId}",
            dto?.id, currentUserId);

        if (dto is null)
        {
            logger.LogWarning("Get team with users failed: request is null");
            return Result<ICollection<TeamMember>>.Failure(
                Errors.Validation("Request cannot be empty."));
        }

        var teamIdValidation = ValidateTeamId(dto.id);
        if (teamIdValidation.IsFailure)
        {
            logger.LogWarning("Get team with users failed: team ID is empty");
            return Result<ICollection<TeamMember>>.Failure(teamIdValidation.Error);
        }

        var userValidation = ValidateUserId(currentUserId);
        if (userValidation.IsFailure)
        {
            logger.LogWarning("Get team with users failed: user ID is empty");
            return Result<ICollection<TeamMember>>.Failure(userValidation.Error);
        }

        var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(dto.id);
        if (team is null)
        {
            logger.LogWarning(
                "Get team with users failed: team {TeamId} not found",
                dto.id);

            return Result<ICollection<TeamMember>>.Failure(
                Errors.NotFound("Team", dto.id.ToString()));
        }

        var isUserInTeam = await unitOfWork.TeamMemberRepository
            .IsUserInTeamAsync(dto.id, currentUserId);

        if (!isUserInTeam)
        {
            logger.LogWarning(
                "Get team with users failed: user {UserId} is not a member of team {TeamId}",
                currentUserId, dto.id);

            return Result<ICollection<TeamMember>>.Failure(
                Errors.Forbidden("You must be a team member to view members."));
        }

        var members = await unitOfWork.TeamRepository.GetTeamWithMembersAsync(dto.id);
        if (members is null || members.Members is null)
        {
            logger.LogWarning(
                "Get team with users failed: no members found for team {TeamId}",
                dto.id);

            return Result<ICollection<TeamMember>>.Failure(
                Errors.NotFound("Team members not found."));
        }

        logger.LogDebug(
            "Found {Count} members in team {TeamId}",
            members.Members.Count, dto.id);

        return Result<ICollection<TeamMember>>.Success(members.Members);
    }

    public async Task<Result> UpdateMemberRoleAsync(Guid teamId, Guid addUserId, Guid userId)
    {
        logger.LogInformation(
            "Update member role request for user {TargetUserId} in team {TeamId} by user {UserId}",
            addUserId, teamId, userId);

        var teamIdValidation = ValidateTeamId(teamId);
        if (teamIdValidation.IsFailure)
        {
            logger.LogWarning("Update member role failed: team ID is empty");
            return teamIdValidation;
        }

        var targetUserValidation = ValidateTargetUserId(
            addUserId,
            "User ID to update cannot be empty.");

        if (targetUserValidation.IsFailure)
        {
            logger.LogWarning("Update member role failed: target user ID is empty");
            return targetUserValidation;
        }

        var userValidation = ValidateUserId(userId);
        if (userValidation.IsFailure)
        {
            logger.LogWarning("Update member role failed: current user ID is empty");
            return Result.Failure(
                Errors.Validation("Current user ID cannot be empty.", "userId"));
        }

        var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(teamId);
        if (team is null)
        {
            logger.LogWarning(
                "Update member role failed: team {TeamId} not found",
                teamId);

            return Result.Failure(Errors.NotFound("Team", teamId.ToString()));
        }

        var isAdmin = await unitOfWork.TeamMemberRepository
            .IsUserAdminAsync(teamId, userId);

        if (!isAdmin)
        {
            logger.LogWarning(
                "Update member role failed: user {UserId} is not admin of team {TeamId}",
                userId, teamId);

            return Result.Failure(
                Errors.Forbidden("Only team administrators can update roles."));
        }

        var teamMember = await unitOfWork.TeamMemberRepository
            .GetByTeamAndUserAsync(teamId, addUserId);

        if (teamMember is null)
        {
            logger.LogWarning(
                "Update member role failed: team member not found for user {TargetUserId} in team {TeamId}",
                addUserId, teamId);

            return Result.Failure(Errors.NotFound("Team member not found."));
        }

        if (teamMember.IsAdmin)
        {
            logger.LogWarning(
                "Update member role failed: user {TargetUserId} is already admin of team {TeamId}",
                addUserId, teamId);

            return Result.Failure(
                Errors.Conflict("User is already a team administrator."));
        }

        teamMember.ChangeRole(true);
        
        var notificationResult =
            await notificationService.NotifyMemberPromotedToAdminAsync(
                addUserId,
                team.Id,
                team.Name);

        if (notificationResult.IsFailure)
            return Result.Failure(notificationResult.Error);

        await unitOfWork.SaveChangesAsync();

        logger.LogInformation(
            "User {TargetUserId} promoted to admin in team {TeamId} by user {UserId}",
            addUserId, team.Id, userId);

        return Result.Success();
    }

    public async Task<Result> ChangeTeamJoinPolicy(
        Guid teamId,
        Guid userId,
        JoinPolicy policy)
    {
        logger.LogInformation(
            "User {UserId} is changing join policy for team {TeamId}",
            userId, teamId);

        var userValidation = ValidateUserId(userId);
        if (userValidation.IsFailure)
        {
            logger.LogWarning(
                "Cannot change team join policy: user ID is empty");

            return userValidation;
        }

        var teamIdValidation = ValidateTeamId(teamId);
        if (teamIdValidation.IsFailure)
        {
            logger.LogWarning(
                "Cannot change team join policy: team ID is empty");

            return teamIdValidation;
        }

        var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(teamId);
        if (team is null)
        {
            logger.LogWarning(
                "Cannot change team join policy: team {TeamId} not found",
                teamId);

            return Result.Failure(
                Errors.NotFound("Team", teamId.ToString()));
        }

        var adminResult = await EnsureTeamAdminAsync(
            userId,
            team.Id,
            "Cannot change team join policy",
            "Only team administrators can change team join policy.");

        if (adminResult.IsFailure)
            return adminResult;

        var changeResult = team.ChangeJoinPolicy(policy);
        if (changeResult.IsFailure)
            return Result.Failure(changeResult.Error);

        await unitOfWork.SaveChangesAsync();

        logger.LogInformation(
            "Join policy for team {TeamId} changed successfully by user {UserId}",
            teamId, userId);

        return Result.Success();
    }
    public async Task<Result> ChangeIdName(Guid userId, Guid teamId, string idName)
    {
        var teamIdValidation = ValidateTeamId(teamId);
        if (teamIdValidation.IsFailure)
        {
            logger.LogWarning("Update member role failed: team ID is empty");
            return teamIdValidation;
        }
        
        if (string.IsNullOrWhiteSpace(idName))
            return Result.Failure(Errors.Validation("Id Name cannot be empty.", "userId"));
        
        var userValidation = ValidateUserId(userId);
        if (userValidation.IsFailure)
        {
            logger.LogWarning("Update member role failed: current user ID is empty");
            return Result.Failure(
                Errors.Validation("Current user ID cannot be empty.", "userId"));
        }
        
        var indexResult = await unitOfWork.TeamRepository.GetByIdNameAsync(idName);
        if (indexResult is not null)
            return Result.Failure(Errors.Conflict("Id exists"));
        
        var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(teamId);
        if (team is null)
        {
            logger.LogWarning(
                "Update member role failed: team {TeamId} not found",
                teamId);

            return Result.Failure(Errors.NotFound("Team", teamId.ToString()));
        }

        var isAdmin = await unitOfWork.TeamMemberRepository
            .IsUserAdminAsync(teamId, userId);

        if (!isAdmin)
        {
            logger.LogWarning(
                "Update member role failed: user {UserId} is not admin of team {TeamId}",
                userId, teamId);

            return Result.Failure(
                Errors.Forbidden("Only team administrators can update roles."));
        }
        
        team.ChangeIdName(idName);
        await unitOfWork.SaveChangesAsync();
        return Result.Success();
    }
    
    // Общие методы валидации

    private static Result ValidateUserId(Guid userId)
    {
        return userId == Guid.Empty
            ? Result.Failure(Errors.Validation("User ID cannot be empty.", "userId"))
            : Result.Success();
    }

    private static Result ValidateTeamId(Guid teamId)
    {
        return teamId == Guid.Empty
            ? Result.Failure(Errors.Validation("Team ID cannot be empty.", "teamId"))
            : Result.Success();
    }

    private static Result ValidateTargetUserId(Guid userId, string errorMessage)
    {
        return userId == Guid.Empty
            ? Result.Failure(Errors.Validation(errorMessage, "userId"))
            : Result.Success();
    }

    // Общая проверка прав администратора

    private async Task<Result> EnsureTeamAdminAsync(
        Guid userId,
        Guid teamId,
        string logMessage,
        string errorMessage)
    {
        var isAdmin = await unitOfWork.TeamRepository
            .IsUserAdminOfTeamAsync(userId, teamId);

        if (isAdmin)
            return Result.Success();

        logger.LogWarning(
            "{Message}: user {UserId} is not admin of team {TeamId}",
            logMessage, userId, teamId);

        return Result.Failure(Errors.Forbidden(errorMessage));
    }

}