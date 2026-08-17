using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using src.Db;
using src.Repository;
using src.UnitOfWork;

namespace TestProject1.Integration_tests;

public class UnitOfWorkEFcoreTest : IAsyncDisposable
{
    public UnitOfWorkEFcore UnitOfWork { get; }
    private readonly ApplicationContext _context;
    private bool _disposed;

    public UnitOfWorkEFcoreTest(ApplicationContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        
        UnitOfWork = new UnitOfWorkEFcore(
            context,
            new UserRepository(context, new NullLogger<UserRepository>()),
            new UserTaskRepository(context, new NullLogger<UserTaskRepository>()),
            new TeamRepository(context, new NullLogger<TeamRepository>()),
            new TeamMemberRepository(context, new NullLogger<TeamMemberRepository>()),
            new NullLogger<UnitOfWorkEFcore>()
        );
    }

    // Упрощённый доступ к контексту
    public ApplicationContext Context => _context;

    // Метод для сохранения изменений
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    // Синхронная версия для совместимости
    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    // Асинхронное освобождение ресурсов
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            await _context.DisposeAsync();
        }
    }

    // Синхронное освобождение для обратной совместимости
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _context.Dispose();
        }
    }
}