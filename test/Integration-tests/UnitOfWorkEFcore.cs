using Microsoft.EntityFrameworkCore;
using src;
using src.Db;
using src.Repository;
using src.UnitOfWork;

namespace TestProject1.Integration_tests;

public class UnitOfWorkEFcoreTest
{
    public UnitOfWorkEFcore UnitOfWork { get; }
    private readonly ApplicationContext _context;

    public UnitOfWorkEFcoreTest(ApplicationContext context)
    {
        _context = context;
        UnitOfWork = new UnitOfWorkEFcore(
            context,
            new UserRepository(context),
            new UserTaskRepository(context),
            new TeamRepository(context),
            new TeamMemberRepository(context)
        );
    }

    // ✅ Упрощённый доступ к контексту
    public ApplicationContext Context => _context;

    // ✅ Метод для очистки контекста (если нужно)
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}