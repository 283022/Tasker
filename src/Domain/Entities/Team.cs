namespace src.Domain.Entities;

public class Team
{
    //for ef core
    private Team()
    {
        
    }
    
    private Team(string name, JoinPolicy policy, string idName)
    {
        JoinPolicy = policy.ToString();
        Name = name;
        IdName = idName;
    }
    
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string JoinPolicy { get; private set; }
    public string IdName { get; private set; }
    
    public ICollection<TeamMember> Members { get;private set; }

    public static Result<Team> Create(string name, JoinPolicy? policy, string idName)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new Error("Validation", "Name cannot be null or empty");

        var joinPolicy = policy ?? Entities.JoinPolicy.All;

        if (!Enum.IsDefined(joinPolicy))
            return new Error("Validation", "Invalid join policy");

        if (string.IsNullOrWhiteSpace(idName))
            return new Error("Validation", "ID Name cannot be null or empty");
        
        return new Team(name, joinPolicy, idName);
    }

    public Result ChangeJoinPolicy(JoinPolicy policy)
    {
        if (!Enum.IsDefined(policy))
            return new Error("Validation", "Invalid join policy");

        JoinPolicy = policy.ToString();
        return Result.Success();
    }

    public Result ChangeIdName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new Error("Validation", "Name cannot be null or empty");
        IdName = name;
        return Result.Success();
    }
}

public enum JoinPolicy
{
    OnlyRequest,
    All
}