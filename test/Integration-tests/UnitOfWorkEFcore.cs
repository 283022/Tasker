using Microsoft.EntityFrameworkCore;
using src;
using src.Db;
using src.Repository;
using src.UnitOfWork;

namespace TestProject1.Integration_tests;

public class UnitOfWorkEFcoreTest(ApplicationContext context)
{
    public  UnitOfWorkEFcore UnitOfWork = new(context,
        new UserRepository(context), new UserTaskRepository(context), 
        new TeamRepository(context),
        new TeamMemberRepository(context));
}