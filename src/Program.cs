using Microsoft.EntityFrameworkCore;
using src;
using src.Auth;
using src.Db;
using src.Repository;
using src.Service;
using src.UnitOfWork;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    var filepath = Path.Combine(AppContext.BaseDirectory, "bin\\Release\\src.xml");
    c.IncludeXmlComments(filepath);
});

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserTaskRepository, UserTaskRepository>();


builder.Services.AddScoped<IUnitOfWork, UnitOfWorkEFcore>();


builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<TeamService>();


builder.Services.AddDbContext<ApplicationContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("ApplicationContext"));
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.MapSwaggerUI();
}

app.UseStatusCodePages();
app.UseExceptionHandler("/error");

app.UseAuthentication();
app.UseAuthorization();
app.AddAllEndpoints();

//ТОЛЬКО ДЛЯ ЭТОГО ПРОЕКТА
//В РЕАЛЬНО ПРОДЕ CI/CD пайплайн должен применять миграцию
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
await db.Database.MigrateAsync();

await app.RunAsync();