using src.Repository;

namespace src.UnitOfWork;

public interface IUnitOfWork
{
    IUserRepository UserRepository { get; }
    IUserTaskRepository UserTaskRepository { get; }
    ITeamRepository TeamRepository { get; }
    ITeamMemberRepository TeamMemberRepository { get; }
    void BeginTransaction();
    void SaveChanges();
    void Commit();
    void Rollback();
}