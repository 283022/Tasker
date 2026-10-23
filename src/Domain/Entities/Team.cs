namespace src.Domain.Entities;

public class Team
{
    //for ef core
    private Team()
    {
        
    }
    
    private Team(string name, JoinPolicy policy)
    {
        JoinPolicy = policy.ToString();
        Name = name;
    }
    
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    
    public string JoinPolicy { get; private set; }
    
    public ICollection<TeamMember> Members { get;private set; }

    public static Result<Team> Create(string name, JoinPolicy? policy)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new Error("Validation", "Name cannot be null or empty");

        var joinPolicy = policy ?? Entities.JoinPolicy.All;

        if (!Enum.IsDefined(joinPolicy))
            return new Error("Validation", "Invalid join policy");

        return new Team(name, joinPolicy);
    }

    public Result<bool> ChangeJoinPolicy(JoinPolicy policy)
    {
        if (!Enum.IsDefined(policy))
            return new Error("Validation", "Invalid join policy");

        JoinPolicy = policy.ToString();
        return true;
    }
    
}

public enum JoinPolicy
{
    OnlyRequest,
    All
}