using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using src;
using src.Auth;
using src.Db;
using src.Domain;
using src.Repository;
using src.Security;
using src.Service;
using src.UnitOfWork;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});


builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(
            namingPolicy: null,
            allowIntegerValues: false
        )
    );
});

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    var filepath = Path.Combine(Directory.GetCurrentDirectory(), "bin\\Debug\\src.xml");
    c.IncludeXmlComments(filepath);

    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tasker", Version = "v1" });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddSerilog();
builder.Services.AddAuthorization();

//Session
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddScoped<SessionService>();

//Repository
builder.Services.AddScoped<IPasswordHasher, Passwordhasher>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserTaskRepository, UserTaskRepository>();
builder.Services.AddScoped<SessionRepository>();
builder.Services.AddScoped<TeamJoinRequestRepository>();
builder.Services.AddScoped<NotificationRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWorkEFcore>();

//Services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<TeamJoinRequestService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<NotificationService>();

builder.Services.AddDbContext<ApplicationContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("ApplicationContext"));
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tasker API v1");
        c.RoutePrefix = "swagger"; // или оставить пустым для корня
    });
    app.UseCors("AllowFrontend");
}

app.UseStatusCodePages();
app.UseExceptionHandler("/error");

app.UseAuthentication();
app.UseAuthorization();
app.AddAllEndpoints();

app.Map("/Error", () =>
        Results.Problem(
            title: "An unexpected error occurred.",
            statusCode: StatusCodes.Status500InternalServerError))
    .AllowAnonymous();

//ТОЛЬКО ДЛЯ ЭТОГО ПРОЕКТА
//В РЕАЛЬНО ПРОДЕ CI/CD пайплайн должен применять миграцию
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
await db.Database.MigrateAsync();

await app.RunAsync();

Log.CloseAndFlush();