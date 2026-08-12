using src.Repository;

namespace src.UnitOfWork;

public interface IUnitOfWork
{
    IUserRepository UserRepository { get; }
    IUserTaskRepository UserTaskRepository { get; }
    ITeamRepository TeamRepository { get; }
    ITeamMemberRepository TeamMemberRepository { get; }
    
    Task BeginTransactionAsync();
    Task<int> SaveChangesAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}