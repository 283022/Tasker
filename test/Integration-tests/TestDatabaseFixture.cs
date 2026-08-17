using Microsoft.EntityFrameworkCore;
using src.Db;

public class TestDatabaseFixture : IAsyncLifetime
{
    private const string ConnectionString = @"Host=localhost;Port=5432;Database=TestDb;Username=postgres;Password=darkrock4;Include Error Detail=true";

    public ApplicationContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new ApplicationContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        
        // Полностью удаляем базу данных
        await context.Database.EnsureDeletedAsync();
        
        // Создаем базу данных заново
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task CleanDatabaseAsync()
    {
        await using var context = CreateContext();
        
        // Отключаем проверку внешних ключей
        await context.Database.ExecuteSqlRawAsync("SET session_replication_role = 'replica';");
        
        // Очищаем все таблицы в правильном порядке
        await context.Database.ExecuteSqlRawAsync(@"
            DO $$ 
            DECLARE
                table_name text;
            BEGIN
                FOR table_name IN 
                    SELECT tablename FROM pg_tables 
                    WHERE schemaname = 'public' 
                    AND tablename NOT IN ('__EFMigrationsHistory')
                LOOP
                    EXECUTE format('TRUNCATE TABLE %I CASCADE', table_name);
                END LOOP;
            END $$;
        ");
        
        // Включаем обратно проверку внешних ключей
        await context.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin';");
    }
}