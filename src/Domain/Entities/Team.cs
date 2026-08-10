namespace src.Domain.Entities;

public class Team
{
    private Team(string name)
    {
        
        Name = name;
    }
    
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    
    public ICollection<TeamMember> Members { get;private set; }

    public static Result<Team> Create(string name)
    {
        if  (string.IsNullOrWhiteSpace(name))
            return new Error("Validation", "Name cannot be null or empty");
       
        return new Team(name);
    }
    
}