using Microsoft.EntityFrameworkCore;
using src.Db;

namespace TestProject1.Integration_tests;

public class TestDatabaseFixture : IDisposable
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
            // Всегда пересоздаем базу при каждом запуске
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
            
            // Полная очистка с правильным порядком
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
            // Если ошибка - пересоздаем базу
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