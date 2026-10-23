using Microsoft.EntityFrameworkCore;
using src.Domain.Entities;

namespace src.Db;

public class ApplicationContext(DbContextOptions<ApplicationContext> options) : DbContext(options)
{
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserTask> Tasks { get; set; }
    
    public DbSet<Session> Sessions {get; set;}
    
    public DbSet<TeamJoinRequest> TeamJoinRequests { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationContext).Assembly);
    }
}