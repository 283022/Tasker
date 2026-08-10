namespace src.Domain.Entities;

public class UserTask
{
    public Guid? AssignedToUserID { get; private set; }
    public Guid? TeamID { get; private set; }
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Guid CreateByUserID { get; private set; }
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

    public Result ChangeName(string newName)
    {
        if (string.IsNullOrEmpty(Name)) return new Error("Validation", "Name cannot be null or empty");
        if (Name.Trim() == "") return new Error("Validation", "Name cannot be null or empty");
        Name = newName;
        return Result.Success();
    }
    public Result AssignTo(Guid userId)
    {
        if(userId.Equals(Guid.Empty))
            return new Error("Validation", "UserId cannot be empty");
        AssignedToUserID = userId;
        return Result.Success();
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