using src.Domain.ValueObjects;

namespace src.Domain.Entities;

public class User
{
    private User()
    {

    }

    private User(string name, Email email, Password password, string displayName, bool visible)
    {
        Name = name;
        Email = email;
        Password = password;
        DisplayName = displayName ?? Guid.NewGuid().ToString();
        Visible = visible;
    }


    public static Result<User> Create(string Name, Email Email, Password Password, string? DisplayName, bool? VisibleValue)
    {
        if (string.IsNullOrEmpty(Name)) return new Error("Validation", "Name cannot be null or empty");
        if (Name.Trim().Length == 0) return new Error("Validation", "Name cannot be empty");
        var displayName = DisplayName ?? Guid.NewGuid().ToString();
        var visible = VisibleValue ?? true;
        return new User(Name, Email, Password, displayName, visible);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string DisplayName { get; private set; }
    public bool Visible { get; private set; } 

    public Email Email { get; private set; }
    public Password Password { get; private set; }

    public void ChangeVisible(bool visible)
    {
        Visible = visible;
    }

    public Result ChangeDisplayName(string newName)
    {
        if (string.IsNullOrEmpty(newName))
            return new Error("Validation", "Name cannot be null or empty");
        DisplayName = newName;
        return Result.Success();
    }

}