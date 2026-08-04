using System.Numerics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace src.Auth;

public class TasklHandlerByUser : AuthorizationHandler<SameAuthorRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SameAuthorRequirement requirement)
    {
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}