namespace src.Domain.Entities;

public class UserTask
{
    public Guid? AssignedToUserID { get; private set; }
    public Guid? TeamID { get; private set; }
    public Guid Id { get; }
    public string Name { get; set; }
    public Guid CreateByUserID { get; init; }
    public bool IsCompleted { get; private set; }

    private UserTask() { }
    private UserTask(string name, Guid createByUserId, Guid? assignedToUserId, Guid? teamId)
    {
        Name = name;
        CreateByUserID = createByUserId;
        IsCompleted = false;
        AssignedToUserID = assignedToUserId;
        TeamID = teamId;
        IsCompleted = false;
    }

    public void AssignTo(Guid userId)
    {
        AssignedToUserID = userId;
    }
    
    public static Result<UserTask> Create(string Name, Guid createByUserId, Guid? teamId = null,
        Guid? assignedToUserId = null)
    {
        if (string.IsNullOrEmpty(Name)) return new Error("Validation", "Name cannot be null or empty");
        if (Name.Trim() == "") return new Error("Validation", "Name cannot be null or empty");
        if (createByUserId.Equals(Guid.Empty)) return new Error("Validation", "CreateByUserId cannot be empty");

        return new UserTask(Name, createByUserId, assignedToUserId, teamId);
    }
}