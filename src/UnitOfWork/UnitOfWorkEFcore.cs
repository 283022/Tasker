using Microsoft.EntityFrameworkCore.Storage;
using src.Db;
using src.Repository;

namespace src.UnitOfWork;

public class UnitOfWorkEFcore : IUnitOfWork, IDisposable
{
    private ApplicationContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public UnitOfWorkEFcore(
        ApplicationContext context,
        IUserRepository userRepository,
        IUserTaskRepository userTaskRepository,
        ITeamRepository teamRepository,
        ITeamMemberRepository teamMemberRepository)
    {
        _context = context;
        UserRepository = userRepository;
        UserTaskRepository = userTaskRepository;
        TeamRepository = teamRepository;
        TeamMemberRepository = teamMemberRepository;
    }

    public IUserRepository UserRepository { get; }
    public IUserTaskRepository UserTaskRepository { get; }
    public ITeamRepository TeamRepository { get; }
    public ITeamMemberRepository TeamMemberRepository { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _transaction?.Dispose();
        _context.Dispose();
        _disposed = true;
    }
}