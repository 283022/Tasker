using src.Domain;
using src.Domain.Entities;
using src.UnitOfWork;

namespace src;

public class TeamService(IUnitOfWork uow)
{
    private readonly IUnitOfWork _unitOfWork = uow;

    public Result Create(Guid userId, CreateTeamDto createTeamDto)
    {
        // 1. Валидация входных данных
        if (userId == Guid.Empty)
            return new Error("Validation", "User Id cannot be empty");
    
        if (createTeamDto is null || string.IsNullOrWhiteSpace(createTeamDto.Name))
            return new Error("Validation", "Team name cannot be empty");

        // 2. Создаем команду
        var teamResult = Team.Create(createTeamDto.Name);
        if (teamResult.IsFailure)
            return teamResult.Error;

        var team = teamResult.Value;

        // 3. Начинаем транзакцию
        _unitOfWork.BeginTransaction();
    
        try
        {
            // 4. Сохраняем команду
            _unitOfWork.TeamRepository.Add(team);
            _unitOfWork.SaveChanges(); // ← Здесь team.Id заполняется (если IDENTITY)

            // 5. Создаем участника (админа)
            var teamMemberResult = TeamMember.Create(team.Id, userId, isAdmin: true);
            if (teamMemberResult.IsFailure)
            {
                _unitOfWork.Rollback();
                return teamMemberResult.Error;
            }

            var teamMember = teamMemberResult.Value;
            _unitOfWork.TeamMemberRepository.Add(teamMember);
        
            // 6. Сохраняем и фиксируем транзакцию
            _unitOfWork.SaveChanges();
            _unitOfWork.Commit();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            return new Error("Database", $"Failed to create team: {ex.Message}");
        }
    }

    public Result Delete(DeleteTeamDto deleteTeamDto, Guid userId)
    {
        if (userId == Guid.Empty)
            return new Error("Validation", "User Id cannot be empty");
    
        if (deleteTeamDto is null)
            return new Error("Validation", "Request cannot be empty");
        
        if (deleteTeamDto.id == Guid.Empty)
            return  new Error("Validation", "Team Id cannot be empty");

        var results = _unitOfWork.TeamRepository.GetTeamById(deleteTeamDto.id);
        if  (results is null)
            return new Error("Validation", "Team not found");
        
        var isUserAdminOfTeam = _unitOfWork.TeamRepository.IsUserAdminOfTeam(userId, deleteTeamDto.id);
        if(!isUserAdminOfTeam)
            return new Error("Validation", "User not admin of team");
        
        _unitOfWork.TeamRepository.Delete(results);
        _unitOfWork.SaveChanges();
        return Result.Success();
    }

    public Result AddUser(AddUserInTeamDto addUserInTeamDto, Guid userId)
    {
        if (userId == Guid.Empty)
            return new Error("Validation", "User Id cannot be empty");
    
        if (addUserInTeamDto is null)
            return new Error("Validation", "Request cannot be empty");
        
        if (addUserInTeamDto.TeamId == Guid.Empty)
            return  new Error("Validation", "Team Id cannot be empty");
        if (addUserInTeamDto.UserId == Guid.Empty)
            return  new Error("Validation", "Team Id cannot be empty");

        var team = _unitOfWork.TeamRepository.GetTeamById(addUserInTeamDto.TeamId);
        if (team is null)
            return new Error("Validation", "Team not found");
        var isUserAdminOfTeam = _unitOfWork.TeamRepository.IsUserAdminOfTeam(userId, team.Id);

        if (!isUserAdminOfTeam)
            return new Error("Validation", "User not admin of team");
        
        var isUserInTeam = _unitOfWork.TeamRepository.IsUserInTeam(addUserInTeamDto.UserId, team.Id);
        if (isUserInTeam)
            return new Error("Validation", "User already in the team");
        
        var teamMember = TeamMember.Create(
            addUserInTeamDto.TeamId, addUserInTeamDto.UserId, addUserInTeamDto.IsAdmin);
        
        if(teamMember.IsFailure)
            return new Error("Database", $"Failed to add user: {teamMember.Error}");
        _unitOfWork.TeamMemberRepository.Add(teamMember.Value);
        _unitOfWork.SaveChanges();
        return Result.Success();
    }

    public Result DeleteUser(DeleteUserFromTeamDto  deleteUserFromTeamDto, Guid userId)
    {
        if (userId == Guid.Empty)
            return new Error("Validation", "User Id cannot be empty");
    
        if (deleteUserFromTeamDto is null)
            return new Error("Validation", "Request cannot be empty");
        
        if (deleteUserFromTeamDto.TeamId == Guid.Empty)
            return  new Error("Validation", "Team Id cannot be empty");
        if (deleteUserFromTeamDto.UserId == Guid.Empty)
            return  new Error("Validation", "Team Id cannot be empty");
        
        var team = _unitOfWork.TeamRepository.GetTeamById(deleteUserFromTeamDto.TeamId);
        if (team is null)
            return new Error("Validation", "Team not found");
        var isUserAdminOfTeam = _unitOfWork.TeamRepository.IsUserAdminOfTeam(userId, team.Id);

        if (!isUserAdminOfTeam)
            return new Error("Validation", "User not admin of team");
        
        var isUserInTeam = _unitOfWork.TeamRepository.IsUserInTeam(deleteUserFromTeamDto.UserId, team.Id);
        if (!isUserInTeam)
            return new Error("Validation", "User not in the team");

        var teamMember = _unitOfWork.TeamMemberRepository.GetByTeamAndUser(
            deleteUserFromTeamDto.TeamId, deleteUserFromTeamDto.UserId);
        
        if (teamMember is null)
            return new Error("Validation", "Team member not found");
        
        _unitOfWork.TeamMemberRepository.Delete(teamMember);
        _unitOfWork.SaveChanges();
        return Result.Success();
    }

    public Result<IEnumerable<TeamMember>> GetAllTeamWhereUserMember(Guid userId)
    {
        if (userId == Guid.Empty)
            return new Error("Validation", "User Id cannot be empty");
        var result = _unitOfWork.TeamMemberRepository.GetByUserId(userId);
        return Result<IEnumerable<TeamMember>>.Success(result);
    }
    public Result<IEnumerable<UserTask>> GetAllTasksTeam(GetAllTeamTaskDto getAllTeamTaskDto, Guid userId)
    {
        // 1. Валидация
        if (userId == Guid.Empty)
            return new Error("Validation", "User Id cannot be empty");
    
        if (getAllTeamTaskDto is null)
            return new Error("Validation", "Request cannot be empty");
    
        if (getAllTeamTaskDto.TeamId == Guid.Empty)
            return new Error("Validation", "Team Id cannot be empty");
    
        // 2. Проверка существования команды
        var team = _unitOfWork.TeamRepository.GetTeamById(getAllTeamTaskDto.TeamId);
        if (team is null)
            return new Error("Validation", "Team not found");
    
        // 3. Проверка членства (достаточно быть участником)
        if (!_unitOfWork.TeamMemberRepository.IsUserInTeam(getAllTeamTaskDto.TeamId, userId))
            return new Error("Authorization", "User is not a member of this team");
    
        // 4. Получаем задачи
        var tasks = _unitOfWork.UserTaskRepository.GetTasksByTeamId(getAllTeamTaskDto.TeamId);
    
        return Result<IEnumerable<UserTask>>.Success(tasks);
    }

    public Result<ICollection<TeamMember>> GetAllTeamWithUsers(GetTeamDto dto, Guid currentUserId)
    {
        // 1. Валидация
        if (dto == null)
            return new Error("Validation", "Request cannot be empty");
        
        if (dto.id == Guid.Empty)
            return new Error("Validation", "Team Id cannot be empty");
        
        if (currentUserId == Guid.Empty)
            return new Error("Validation", "User Id cannot be empty");

        // 2. Проверка существования команды
        var team = _unitOfWork.TeamRepository.GetTeamById(dto.id);
        if (team is null)
            return new Error("Validation", "Team not found");

        // 3. Проверка прав (пользователь должен быть участником команды)
        if (!_unitOfWork.TeamMemberRepository.IsUserInTeam(dto.id, currentUserId))
            return new Error("Authorization", "User is not a member of this team");

        // 4. Получаем всех участников
        var members = _unitOfWork.TeamRepository.GetTeamWithMembers(dto.id);
        if(members is null)
            return new Error("Validation", "Members not found");
        
        return Result<ICollection<TeamMember>>.Success(members.Members);
    }

    public Result UpdateMemberRole(UpdateTeamRoleDto dto, Guid userId)
    {
        if (dto is null)
            return new Error("Validation", "Request cannot be empty");
        if (dto.TeamId == Guid.Empty)
            return new Error("Validation", "Id cannot be empty");
        if (dto.UserId == Guid.Empty)
            return new Error("Validation", "Id cannot be empty");
        if (userId == Guid.Empty)
            return new Error("Validation", "Id cannot be empty");
        
        var team = _unitOfWork.TeamRepository.GetTeamById(dto.TeamId);
        if (team is null)
            return new Error("Validation", "Team not found");

        if (!_unitOfWork.TeamMemberRepository.IsUserAdmin(dto.TeamId, userId))
            return new Error("Authorization", "User is not a member of this team");

        var teamMember = _unitOfWork.TeamMemberRepository.GetByTeamAndUser(dto.TeamId,dto.UserId );

        if (teamMember is null)
            return new Error("Validation", "Not found");

        if (teamMember.IsAdmin)
            return new Error("BadRequest", "User already is admin");

        teamMember.ChangeRole(true);
        _unitOfWork.SaveChanges();
        return Result.Success();
    }
}