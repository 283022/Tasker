namespace src.UnitOfWork;

public interface IUnitOfWork
{
    IUserRepository UserRepository { get; }
    IUserTaskRepository UserTaskRepository { get; }
    void BeginTransaction();
    void SaveChanges();
    void Commit();
    void Rollback();
}