namespace src.Domain.Entities;

public class UserTask
{
    private UserTask(string name, Guid createByUserId)
    {
        Name = name;
        CreateByUserID = createByUserId;
    }

    public static Result<UserTask> Create(string Name, Guid createByUserId)
    {
        if (string.IsNullOrEmpty(Name)) return new Error("Validation", "Name cannot be null or empty");
        if (createByUserId.Equals(Guid.Empty)) return new Error("Validation", "CreateByUserId cannot be empty");
        
        return new UserTask(Name, createByUserId);
        
    }
    
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } 
    public Guid CreateByUserID { get; init; } 
}