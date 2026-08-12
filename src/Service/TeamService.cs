using src.Domain;
using src.Domain.Entities;
using src.UnitOfWork;

namespace src;

public class TeamService(IUnitOfWork uow)
{
    private readonly IUnitOfWork _unitOfWork = uow;

    public async Task<Result> CreateAsync(Guid userId, CreateTeamDto createTeamDto)
    {
        // 1. Валидация входных данных
        if (userId == Guid.Empty)
            return Result.Failure(new Error("Validation", "User Id cannot be empty"));
    
        if (createTeamDto is null || string.IsNullOrWhiteSpace(createTeamDto.Name))
            return Result.Failure(new Error("Validation", "Team name cannot be empty"));

        // 2. Создаем команду
        var teamResult = Team.Create(createTeamDto.Name);
        if (teamResult.IsFailure)
            return Result.Failure(teamResult.Error);

        var team = teamResult.Value;

        // 3. Начинаем транзакцию
        await _unitOfWork.BeginTransactionAsync();
    
        try
        {
            // 4. Сохраняем команду
            await _unitOfWork.TeamRepository.AddTeamAsync(team);
            await _unitOfWork.SaveChangesAsync(); // ← Здесь team.Id заполняется (если IDENTITY)

            // 5. Создаем участника (админа)
            var teamMemberResult = TeamMember.Create(team.Id, userId, isAdmin: true);
            if (teamMemberResult.IsFailure)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result.Failure(teamMemberResult.Error);
            }

            var teamMember = teamMemberResult.Value;
            await _unitOfWork.TeamMemberRepository.AddTeamMemberAsync(teamMember);
        
            // 6. Сохраняем и фиксируем транзакцию
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure(new Error("Database", $"Failed to create team: {ex.Message}"));
        }
    }

    public async Task<Result> DeleteAsync(DeleteTeamDto deleteTeamDto, Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure(new Error("Validation", "User Id cannot be empty"));
    
        if (deleteTeamDto is null)
            return Result.Failure(new Error("Validation", "Request cannot be empty"));
        
        if (deleteTeamDto.id == Guid.Empty)
            return Result.Failure(new Error("Validation", "Team Id cannot be empty"));

        var team = await _unitOfWork.TeamRepository.GetTeamByIdAsync(deleteTeamDto.id);
        if (team is null)
            return Result.Failure(new Error("Validation", "Team not found"));
        
        var isUserAdminOfTeam = await _unitOfWork.TeamRepository.IsUserAdminOfTeamAsync(userId, deleteTeamDto.id);
        if (!isUserAdminOfTeam)
            return Result.Failure(new Error("Validation", "User not admin of team"));
        
        await _unitOfWork.TeamRepository.DeleteTeamAsync(team);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> AddUserAsync(AddUserInTeamDto addUserInTeamDto, Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure(new Error("Validation", "User Id cannot be empty"));
    
        if (addUserInTeamDto is null)
            return Result.Failure(new Error("Validation", "Request cannot be empty"));
        
        if (addUserInTeamDto.TeamId == Guid.Empty)
            return Result.Failure(new Error("Validation", "Team Id cannot be empty"));
        if (addUserInTeamDto.UserId == Guid.Empty)
            return Result.Failure(new Error("Validation", "User Id cannot be empty"));

        var team = await _unitOfWork.TeamRepository.GetTeamByIdAsync(addUserInTeamDto.TeamId);
        if (team is null)
            return Result.Failure(new Error("Validation", "Team not found"));
        
        var isUserAdminOfTeam = await _unitOfWork.TeamRepository.IsUserAdminOfTeamAsync(userId, team.Id);
        if (!isUserAdminOfTeam)
            return Result.Failure(new Error("Validation", "User not admin of team"));
        
        var isUserInTeam = await _unitOfWork.TeamRepository.IsUserInTeamAsync(addUserInTeamDto.UserId, team.Id);
        if (isUserInTeam)
            return Result.Failure(new Error("Validation", "User already in the team"));
        
        var teamMember = TeamMember.Create(
            addUserInTeamDto.TeamId, addUserInTeamDto.UserId, addUserInTeamDto.IsAdmin);
        
        if (teamMember.IsFailure)
            return Result.Failure(new Error("Database", $"Failed to add user: {teamMember.Error}"));
        
        await _unitOfWork.TeamMemberRepository.AddTeamMemberAsync(teamMember.Value);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(DeleteUserFromTeamDto deleteUserFromTeamDto, Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure(new Error("Validation", "User Id cannot be empty"));
    
        if (deleteUserFromTeamDto is null)
            return Result.Failure(new Error("Validation", "Request cannot be empty"));
        
        if (deleteUserFromTeamDto.TeamId == Guid.Empty)
            return Result.Failure(new Error("Validation", "Team Id cannot be empty"));
        if (deleteUserFromTeamDto.UserId == Guid.Empty)
            return Result.Failure(new Error("Validation", "User Id cannot be empty"));
        
        var team = await _unitOfWork.TeamRepository.GetTeamByIdAsync(deleteUserFromTeamDto.TeamId);
        if (team is null)
            return Result.Failure(new Error("Validation", "Team not found"));
        
        var isUserAdminOfTeam = await _unitOfWork.TeamRepository.IsUserAdminOfTeamAsync(userId, team.Id);
        if (!isUserAdminOfTeam)
            return Result.Failure(new Error("Validation", "User not admin of team"));
        
        var isUserInTeam = await _unitOfWork.TeamRepository.IsUserInTeamAsync(deleteUserFromTeamDto.UserId, team.Id);
        if (!isUserInTeam)
            return Result.Failure(new Error("Validation", "User not in the team"));

        var teamMember = await _unitOfWork.TeamMemberRepository.GetByTeamAndUserAsync(
            deleteUserFromTeamDto.TeamId, deleteUserFromTeamDto.UserId);
        
        if (teamMember is null)
            return Result.Failure(new Error("Validation", "Team member not found"));
        
        await _unitOfWork.TeamMemberRepository.DeleteTeamMemberAsync(teamMember);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<IEnumerable<TeamMember>>> GetAllTeamWhereUserMemberAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result<IEnumerable<TeamMember>>.Failure(new Error("Validation", "User Id cannot be empty"));
        
        var result = await _unitOfWork.TeamMemberRepository.GetByUserIdAsync(userId);
        return Result<IEnumerable<TeamMember>>.Success(result);
    }

    public async Task<Result<IEnumerable<UserTask>>> GetAllTasksTeamAsync(GetAllTeamTaskDto getAllTeamTaskDto, Guid userId)
    {
        // 1. Валидация
        if (userId == Guid.Empty)
            return Result<IEnumerable<UserTask>>.Failure(new Error("Validation", "User Id cannot be empty"));
    
        if (getAllTeamTaskDto is null)
            return Result<IEnumerable<UserTask>>.Failure(new Error("Validation", "Request cannot be empty"));
    
        if (getAllTeamTaskDto.TeamId == Guid.Empty)
            return Result<IEnumerable<UserTask>>.Failure(new Error("Validation", "Team Id cannot be empty"));
    
        // 2. Проверка существования команды
        var team = await _unitOfWork.TeamRepository.GetTeamByIdAsync(getAllTeamTaskDto.TeamId);
        if (team is null)
            return Result<IEnumerable<UserTask>>.Failure(new Error("Validation", "Team not found"));
    
        // 3. Проверка членства (достаточно быть участником)
        var isUserInTeam = await _unitOfWork.TeamMemberRepository.IsUserInTeamAsync(getAllTeamTaskDto.TeamId, userId);
        if (!isUserInTeam)
            return Result<IEnumerable<UserTask>>.Failure(new Error("Authorization", "User is not a member of this team"));
    
        // 4. Получаем задачи
        var tasks = await _unitOfWork.UserTaskRepository.GetTasksByTeamIdAsync(getAllTeamTaskDto.TeamId);
    
        return Result<IEnumerable<UserTask>>.Success(tasks);
    }

    public async Task<Result<ICollection<TeamMember>>> GetAllTeamWithUsersAsync(GetTeamDto dto, Guid currentUserId)
    {
        // 1. Валидация
        if (dto == null)
            return Result<ICollection<TeamMember>>.Failure(new Error("Validation", "Request cannot be empty"));
        
        if (dto.id == Guid.Empty)
            return Result<ICollection<TeamMember>>.Failure(new Error("Validation", "Team Id cannot be empty"));
        
        if (currentUserId == Guid.Empty)
            return Result<ICollection<TeamMember>>.Failure(new Error("Validation", "User Id cannot be empty"));

        // 2. Проверка существования команды
        var team = await _unitOfWork.TeamRepository.GetTeamByIdAsync(dto.id);
        if (team is null)
            return Result<ICollection<TeamMember>>.Failure(new Error("Validation", "Team not found"));

        // 3. Проверка прав (пользователь должен быть участником команды)
        var isUserInTeam = await _unitOfWork.TeamMemberRepository.IsUserInTeamAsync(dto.id, currentUserId);
        if (!isUserInTeam)
            return Result<ICollection<TeamMember>>.Failure(new Error("Authorization", "User is not a member of this team"));

        // 4. Получаем всех участников
        var members = await _unitOfWork.TeamRepository.GetTeamWithMembersAsync(dto.id);
        if (members is null || members.Members is null)
            return Result<ICollection<TeamMember>>.Failure(new Error("Validation", "Members not found"));
        
        return Result<ICollection<TeamMember>>.Success(members.Members);
    }

    public async Task<Result> UpdateMemberRoleAsync(UpdateTeamRoleDto dto, Guid userId)
    {
        if (dto is null)
            return Result.Failure(new Error("Validation", "Request cannot be empty"));
        
        if (dto.TeamId == Guid.Empty)
            return Result.Failure(new Error("Validation", "Team Id cannot be empty"));
        
        if (dto.UserId == Guid.Empty)
            return Result.Failure(new Error("Validation", "User Id cannot be empty"));
        
        if (userId == Guid.Empty)
            return Result.Failure(new Error("Validation", "Current User Id cannot be empty"));
        
        var team = await _unitOfWork.TeamRepository.GetTeamByIdAsync(dto.TeamId);
        if (team is null)
            return Result.Failure(new Error("Validation", "Team not found"));

        var isAdmin = await _unitOfWork.TeamMemberRepository.IsUserAdminAsync(dto.TeamId, userId);
        if (!isAdmin)
            return Result.Failure(new Error("Authorization", "User is not an admin of this team"));

        var teamMember = await _unitOfWork.TeamMemberRepository.GetByTeamAndUserAsync(dto.TeamId, dto.UserId);
        if (teamMember is null)
            return Result.Failure(new Error("Validation", "Team member not found"));

        if (teamMember.IsAdmin)
            return Result.Failure(new Error("BadRequest", "User already is admin"));

        teamMember.ChangeRole(true);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }
}