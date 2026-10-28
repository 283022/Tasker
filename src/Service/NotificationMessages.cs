namespace src.Service;

public static class NotificationMessages
{
    public static string TaskAssigned(string taskTitle)
        => $"Вам назначена задача «{taskTitle}».";

    public static string RemovedFromGroup(string teamName)
        => $"Вас удалили из группы «{teamName}».";

    public static string InvitationAccepted(string teamName)
        => $"Ваше приглашение в группу «{teamName}» принято.";
    
    public static string AddedToGroup(string teamName)
        => $"Вас добавили в группу «{teamName}».";

    public static string TeamDeleted(string teamName)
        => $"Группа «{teamName}» была удалена.";

    public static string MemberPromotedToAdmin(string teamName)
        => $"Вам выдали права администратора в группе «{teamName}».";

    public static string TeamJoinPolicyChanged(
        string teamName,
        string policy)
        => $"Изменены правила вступления в группу «{teamName}»: {policy}.";

    public static string TeamIdNameChanged(
        string teamName,
        string idName)
        => $"Короткое имя группы «{teamName}» изменено на «{idName}».";
    
    public static string TaskDeleted(string taskTitle)
        => $"Задача «{taskTitle}» была удалена.";

    public static string TaskRenamed(string oldTitle, string newTitle)
        => $"Название задачи изменено с «{oldTitle}» на «{newTitle}».";

    public static string TaskReassigned(string taskTitle)
        => $"Задача «{taskTitle}» переназначена другому участнику.";
    
    public static string JoinRequestApproved(string teamName)
        => $"Ваша заявка на вступление в команду «{teamName}» одобрена.";

    public static string JoinRequestRejected(string teamName)
        => $"Ваша заявка на вступление в команду «{teamName}» отклонена.";
    
}