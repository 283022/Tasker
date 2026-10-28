using src.Repository;

namespace src.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IUserRepository UserRepository { get; }
    IUserTaskRepository UserTaskRepository { get; }
    ITeamRepository TeamRepository { get; }
    TeamJoinRequestRepository TeamJoinRequestRepository { get; }
    ITeamMemberRepository TeamMemberRepository { get; }
    NotificationRepository NotificationRepository { get;  }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}