using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace TeamStride.Api.Authorization;

/// <summary>
/// Authorization attribute that requires the current user to have global admin privileges.
/// Checks the 'is_global_admin' claim in the JWT token and denies access if the user is not a global admin.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireGlobalAdminAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check for is_global_admin claim
        var isGlobalAdminClaim = context.HttpContext.User.FindFirst("is_global_admin");
        
        if (isGlobalAdminClaim == null)
        {
            // If claim is missing, deny access
            context.Result = new ForbidResult();
            return;
        }

        // Check if the claim value is "true"
        if (!bool.TryParse(isGlobalAdminClaim.Value, out var isGlobalAdmin) || !isGlobalAdmin)
        {
            // If claim is not "true", deny access
            context.Result = new ForbidResult();
            return;
        }

        // User is authenticated and has global admin privileges - allow access
    }
} 