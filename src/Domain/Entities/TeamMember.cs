namespace src.Domain.Entities;

public class TeamMember
{
    //ef core
    private TeamMember()
    {
    }

    private TeamMember(Guid teamId, Guid userId, bool isAdmin)
    {
        TeamId = teamId;
        UserId = userId;
        IsAdmin = isAdmin;
    }

    public static Result<TeamMember> Create(Guid teamId, Guid userId, bool isAdmin)
    {
        return new TeamMember(teamId, userId, isAdmin);
    }

    public void ChangeRole(bool isAdmin)
    {
        IsAdmin = isAdmin;
    }
    
    
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsAdmin { get; private set; }
    
    
    public User User { get; private set; }
}