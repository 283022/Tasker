using Microsoft.EntityFrameworkCore.Storage;
using src.Db;
using src.Repository;
using Microsoft.Extensions.Logging;

namespace src.UnitOfWork;

public class UnitOfWorkEFcore : IUnitOfWork, IDisposable
{
    private readonly ApplicationContext _context;
    private readonly ILogger<UnitOfWorkEFcore> _logger;
    private IDbContextTransaction? _transaction;
    private bool _disposed;
    

    public UnitOfWorkEFcore(
        ApplicationContext context,
        IUserRepository userRepository,
        IUserTaskRepository userTaskRepository,
        ITeamRepository teamRepository,
        ITeamMemberRepository teamMemberRepository,
        ILogger<UnitOfWorkEFcore> logger)
    {
        _context = context;
        _logger = logger;
        UserRepository = userRepository;
        UserTaskRepository = userTaskRepository;
        TeamRepository = teamRepository;
        TeamMemberRepository = teamMemberRepository;
    }

    public IUserRepository UserRepository { get; }
    public IUserTaskRepository UserTaskRepository { get; }
    public ITeamRepository TeamRepository { get; }
    
    public TeamJoinRequestRepository TeamJoinRequestRepository { get; }
    public ITeamMemberRepository TeamMemberRepository { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Saving changes to database");
        try
        {
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogTrace("SaveChanges completed: {Count} entities saved", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            throw;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Beginning database transaction");
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogTrace("Database transaction started");
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            _logger.LogWarning("Attempted to commit transaction but no transaction exists");
            return;
        }

        try
        {
            _logger.LogTrace("Committing database transaction");
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
            _logger.LogTrace("Database transaction committed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing database transaction");
            throw;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            _logger.LogWarning("Attempted to rollback transaction but no transaction exists");
            return;
        }

        try
        {
            _logger.LogWarning("Rolling back database transaction");
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
            _logger.LogWarning("Database transaction rolled back");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back database transaction");
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _logger.LogTrace("Disposing UnitOfWork");
            _transaction?.Dispose();
            _context.Dispose();
            _disposed = true;
            _logger.LogTrace("UnitOfWork disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing UnitOfWork");
        }
    }
}