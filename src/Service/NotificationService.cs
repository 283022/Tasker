using src.Domain;
using src.Domain.Entities;
using src.UnitOfWork;

namespace src.Service;

public class NotificationService(IUnitOfWork unitOfWork)
{
    public async Task<Result> NotifyTaskAssignedAsync(
        Guid recipientId,
        Guid taskId,
        string taskTitle,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            recipientId,
            NotificationType.TaskAssigned,
            NotificationMessages.TaskAssigned(taskTitle),
            taskId: taskId);

        if (notification.IsFailure)
            return Result.Failure(notification.Error);

        await unitOfWork.NotificationRepository.AddNotificationAsync(notification.Value, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> NotifyUserRemovedFromGroupAsync(
        Guid recipientId,
        Guid teamId,
        string teamName,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            recipientId,
            NotificationType.RemovedFromGroup,
            NotificationMessages.RemovedFromGroup(teamName),
            teamId: teamId);


        if (notification.IsFailure)
            return Result.Failure(notification.Error);

        await unitOfWork.NotificationRepository
            .AddNotificationAsync(notification.Value, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> NotifyInvitationAcceptedAsync(
        Guid recipientId,
        Guid teamId,
        string teamName,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            recipientId,
            NotificationType.InvitationAccepted,
            NotificationMessages.InvitationAccepted(teamName),
            teamId: teamId);


        if (notification.IsFailure)
            return Result.Failure(notification.Error);

        await unitOfWork.NotificationRepository
            .AddNotificationAsync(notification.Value, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> NotifyUserAddedToGroupAsync(
        Guid recipientId,
        Guid teamId,
        string teamName,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            recipientId,
            NotificationType.AddedToGroup,
            NotificationMessages.AddedToGroup(teamName),
            teamId: teamId);


        if (notification.IsFailure)
            return Result.Failure(notification.Error);

        await unitOfWork.NotificationRepository
            .AddNotificationAsync(notification.Value, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> NotifyTeamDeletedAsync(
        Guid recipientId,
        Guid teamId,
        string teamName,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            recipientId,
            NotificationType.TeamDeleted,
            NotificationMessages.TeamDeleted(teamName),
            teamId: teamId);


        if (notification.IsFailure)
            return Result.Failure(notification.Error);

        await unitOfWork.NotificationRepository
            .AddNotificationAsync(notification.Value, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> NotifyMemberPromotedToAdminAsync(
        Guid recipientId,
        Guid teamId,
        string teamName,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            recipientId,
            NotificationType.MemberPromotedToAdmin,
            NotificationMessages.MemberPromotedToAdmin(teamName),
            teamId: teamId);

        if (notification.IsFailure)
            return Result.Failure(notification.Error);
        
        await unitOfWork.NotificationRepository
            .AddNotificationAsync(notification.Value, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> NotifyTeamJoinPolicyChangedAsync(
        Guid recipientId,
        Guid teamId,
        string teamName,
        string policy,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            recipientId,
            NotificationType.TeamJoinPolicyChanged,
            NotificationMessages.TeamJoinPolicyChanged(teamName, policy),
            teamId: teamId);

        if (notification.IsFailure)
            return Result.Failure(notification.Error);
        
        await unitOfWork.NotificationRepository
            .AddNotificationAsync(notification.Value, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> NotifyTeamIdNameChangedAsync(
        Guid recipientId,
        Guid teamId,
        string teamName,
        string idName,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            recipientId,
            NotificationType.TeamIdNameChanged,
            NotificationMessages.TeamIdNameChanged(teamName, idName),
            teamId: teamId);

        if (notification.IsFailure)
            return Result.Failure(notification.Error);
        
        await unitOfWork.NotificationRepository
            .AddNotificationAsync(notification.Value, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> NotifyTaskDeletedAsync(
        Guid recipientId,
        string taskTitle,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            recipientId,
            NotificationType.TaskDeleted,
            NotificationMessages.TaskDeleted(taskTitle));

        if (notification.IsFailure)
            return Result.Failure(notification.Error);
        
        await unitOfWork.NotificationRepository
            .AddNotificationAsync(notification.Value, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> NotifyTaskRenamedAsync(
        Guid recipientId,
        Guid taskId,
        string oldTitle,
        string newTitle,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            recipientId,
            NotificationType.TaskRenamed,
            NotificationMessages.TaskRenamed(oldTitle, newTitle),
            taskId: taskId);

        if (notification.IsFailure)
            return Result.Failure(notification.Error);
        
        await unitOfWork.NotificationRepository
            .AddNotificationAsync(notification.Value, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> NotifyTaskReassignedAsync(
        Guid recipientId,
        Guid taskId,
        string taskTitle,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            recipientId,
            NotificationType.TaskReassigned,
            NotificationMessages.TaskReassigned(taskTitle),
            taskId: taskId);

        if (notification.IsFailure)
            return Result.Failure(notification.Error);
        
        await unitOfWork.NotificationRepository
            .AddNotificationAsync(notification.Value, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> NotifyJoinRequestApprovedAsync(
        Guid recipientId,
        Guid teamId,
        string teamName,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            recipientId,
            NotificationType.JoinRequestApproved,
            NotificationMessages.JoinRequestApproved(teamName),
            teamId: teamId);

        if (notification.IsFailure)
            return Result.Failure(notification.Error);
        
        await unitOfWork.NotificationRepository
            .AddNotificationAsync(notification.Value, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> NotifyJoinRequestRejectedAsync(
        Guid recipientId,
        Guid teamId,
        string teamName,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            recipientId,
            NotificationType.JoinRequestRejected,
            NotificationMessages.JoinRequestRejected(teamName),
            teamId: teamId);

        if (notification.IsFailure)
            return Result.Failure(notification.Error);
        
        await unitOfWork.NotificationRepository
            .AddNotificationAsync(notification.Value, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<NotificationsUserDto>> GetAllUserNotifyAsync(
        Guid userId,
        PaginationDto paginationDto,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Result<NotificationsUserDto>.Failure(
                Errors.Validation("User ID cannot be empty.", "userId"));

        var skip = Math.Max(0, paginationDto.Skip);
        var take = Math.Clamp(paginationDto.Take, 1, 100);
        
        
        var notifications = await unitOfWork.NotificationRepository
            .GetByUserIdAsync(
                userId,
                skip,
                take,
                cancellationToken);

        var notificationDtos = notifications
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Message ?? "",
                n.TaskId,
                n.TeamId,
                n.CreatedAt,
                n.ReadAt))
            .ToList();

        var result = new NotificationsUserDto(notificationDtos);

        return Result<NotificationsUserDto>.Success(result);
    }
    
    public async Task<Result> MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (notificationId == Guid.Empty)
            return Result.Failure(
                Errors.Validation(
                    "Notification ID cannot be empty.",
                    "notificationId"));

        if (userId == Guid.Empty)
            return Result.Failure(
                Errors.Validation(
                    "User ID cannot be empty.",
                    "userId"));

        var notification = await unitOfWork.NotificationRepository
            .GetByIdForUserAsync(
                notificationId,
                userId,
                cancellationToken);

        if (notification is null)
            return Result.Failure(
                Errors.NotFound("Notification", notificationId.ToString()));

        notification.MarkAsRead();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
    public async Task<Result<int>> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Result<int>.Failure(
                Errors.Validation(
                    "User ID cannot be empty.",
                    "userId"));

        var count = await unitOfWork.NotificationRepository
            .CountUnreadAsync(userId, cancellationToken);

        return Result<int>.Success(count);
    }
    
    public async Task<Result<int>> GetTotalCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Result<int>.Failure(
                Errors.Validation(
                    "User ID cannot be empty.",
                    "userId"));

        var count = await unitOfWork.NotificationRepository
            .CountByUserIdAsync(userId, cancellationToken);

        return Result<int>.Success(count);
    }
}