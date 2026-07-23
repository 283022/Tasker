namespace Authorization2;

public class UserTask(string Name, Guid userid)
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = Name;
    public Guid CreateByUserID { get; init; } = userid;
}