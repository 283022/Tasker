using System.Text;
using src;
using src.Auth;
using src.Mappers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSingleton<UserRepository>();


builder.Services.AddOpenApi();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme,options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("qwertyuiop[]asdfghjkl;'zxcvbnm,./"))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("zxc", out var token))
            {
                context.Token = token;
            }

            return Task.CompletedTask;
        }
    };
});


builder.Services.AddSingleton<TaskRepository>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddScoped<MapperTaskFromDto>();
builder.Services.AddScoped<MapperUserFromDto>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<IAuthorizationHandler,TasklHandlerByUser>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AllNameOfPolicy.SameAuthorRequirement, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new SameAuthorRequirement());
    });
});

builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.Run();