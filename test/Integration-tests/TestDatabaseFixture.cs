using Microsoft.EntityFrameworkCore;
using src.Db;

namespace TestProject1.Integration_tests;

public class TestDatabaseFixture : IDisposable, IAsyncDisposable
{
    private const string ConnectionString =
        @"Host=localhost;Port=5432;Database=TestDb;Username=postgres;Password=darkrock4;Include Error Detail=true";
    
    private ApplicationContext _context;
    private bool _disposed;
    private static readonly object _lock = new object();

    public TestDatabaseFixture()
    {
        lock (_lock)
        {
            _context = CreateContext();
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }
    }

    public ApplicationContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        
        return new ApplicationContext(options);
    }

    public ApplicationContext CreateNewContext()
    {
        return CreateContext();
    }

    public void CleanDatabase()
    {
        try
        {
            using var context = CreateNewContext();
            context.Database.ExecuteSqlRaw(@"
                DO $$ 
                BEGIN
                    EXECUTE 'SET session_replication_role = replica';
                    
                    TRUNCATE TABLE ""UserTasks"" CASCADE;
                    TRUNCATE TABLE ""TeamMembers"" CASCADE;
                    TRUNCATE TABLE ""Tasks"" CASCADE;
                    TRUNCATE TABLE ""Teams"" CASCADE;
                    TRUNCATE TABLE ""Users"" CASCADE;
                    
                    EXECUTE 'SET session_replication_role = origin';
                END $$;
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CleanDatabase error: {ex.Message}");
            try
            {
                _context.Database.EnsureDeleted();
                _context.Database.EnsureCreated();
            }
            catch
            {
                // Игнорируем
            }
        }
    }

    public async Task CleanDatabaseAsync()
    {
        try
        {
            await using var context = CreateNewContext();
            await context.Database.ExecuteSqlRawAsync(@"
                DO $$ 
                BEGIN
                    EXECUTE 'SET session_replication_role = replica';
                    
                    TRUNCATE TABLE ""UserTasks"" CASCADE;
                    TRUNCATE TABLE ""TeamMembers"" CASCADE;
                    TRUNCATE TABLE ""Tasks"" CASCADE;
                    TRUNCATE TABLE ""Teams"" CASCADE;
                    TRUNCATE TABLE ""Users"" CASCADE;
                    
                    EXECUTE 'SET session_replication_role = origin';
                END $$;
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CleanDatabaseAsync error: {ex.Message}");
            try
            {
                await _context.Database.EnsureDeletedAsync();
                await _context.Database.EnsureCreatedAsync();
            }
            catch
            {
                // Игнорируем
            }
        }
    }

    // ✅ Вспомогательный метод для создания чистого контекста
    public async Task<ApplicationContext> GetCleanContextAsync()
    {
        var context = CreateNewContext();
        await CleanDatabaseAsync();
        return context;
    }

    // ✅ Вспомогательный метод для создания UnitOfWork с новым контекстом
    public async Task<(ApplicationContext Context, UnitOfWorkEFcoreTest UnitOfWork)> CreateCleanUnitOfWorkAsync()
    {
        var context = await GetCleanContextAsync();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        return (context, unitOfWork);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            lock (_lock)
            {
                try
                {
                    _context.Dispose();
                }
                catch
                {
                    // Игнорируем
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
        GC.SuppressFinalize(this);
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lock)
            {
                try
                {
                    _context.Dispose();
                }
                catch
                {
                    // Игнорируем
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
        GC.SuppressFinalize(this);
    }
}