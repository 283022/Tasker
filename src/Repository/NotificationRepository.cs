using Microsoft.EntityFrameworkCore;
using src.Db;
using src.Domain.Entities;

namespace src.Repository;

public class NotificationRepository(
    ApplicationContext context,
    ILogger<NotificationRepository> logger)
{
    public async Task AddNotificationAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Adding notification {NotificationId} for user {UserId}",
            notification.Id,
            notification.UserId);

        await context.Notifications.AddAsync(
            notification,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 100);

        logger.LogDebug(
            "Fetching notifications for user {UserId}",
            userId);

        return await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<Notification?> GetByIdForUserAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Fetching notification {NotificationId} for user {UserId}",
            notificationId,
            userId);

        return await context.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == notificationId && n.UserId == userId,
                cancellationToken);
    }

    public async Task<int> CountUnreadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
       return await context.Notifications
            .CountAsync(
                n => n.UserId == userId && n.ReadAt == null,
                cancellationToken);
    }
    
    public async Task<int> CountByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await context.Notifications
            .CountAsync(
                n => n.UserId == userId,
                cancellationToken);
    }
}