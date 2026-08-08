using Microsoft.EntityFrameworkCore.Storage;
using src.Db;

namespace src.UnitOfWork;

public class UnitOfWorkEFcore: IUnitOfWork
{
    private ApplicationContext  _context;
    private IDbContextTransaction? _transaction;
    
    public UnitOfWorkEFcore(ApplicationContext context, IUserRepository userRepository, IUserTaskRepository userTaskRepository)
    {
        _context = context;
        UserRepository = userRepository;
        UserTaskRepository = userTaskRepository;
    }
    public IUserRepository UserRepository { get; }
    public IUserTaskRepository UserTaskRepository { get; }
    public void SaveChanges()
    {
        _context.SaveChanges();
    }

    public void BeginTransaction()
    {
        _transaction = _context.Database.BeginTransaction();
    }
    
    public void Commit()
    {
        _transaction?.Commit();
    }

    public void Rollback()
    {
        _transaction?.Rollback();
    }
}