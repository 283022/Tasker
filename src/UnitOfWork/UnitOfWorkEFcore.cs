using Microsoft.EntityFrameworkCore.Storage;
using src.Db;
using src.Repository;

namespace src.UnitOfWork;

public class UnitOfWorkEFcore: IUnitOfWork, IDisposable
{
    private ApplicationContext  _context;
    private IDbContextTransaction? _transaction;
    
    public UnitOfWorkEFcore(ApplicationContext context, IUserRepository userRepository, IUserTaskRepository userTaskRepository, ITeamRepository teamRepository, ITeamMemberRepository teamMemberRepository)
    {
        _context = context;
        UserRepository = userRepository;
        UserTaskRepository = userTaskRepository;
        TeamRepository = teamRepository;
        TeamMemberRepository = teamMemberRepository;
    }
    public IUserRepository UserRepository { get; }
    public IUserTaskRepository UserTaskRepository { get; }
    public ITeamRepository TeamRepository { get;  }
    public ITeamMemberRepository TeamMemberRepository { get; }
    
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
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}