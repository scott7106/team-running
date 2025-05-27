using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using TeamStride.Domain.Entities;

namespace TeamStride.Api.Authorization;

/// <summary>
/// Authorization attribute that requires the current user to have access to a specific team
/// with the specified minimum role level. Supports role hierarchy where higher roles 
/// automatically satisfy lower role requirements.
/// Global admins bypass all team access restrictions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireTeamAccessAttribute : Attribute, IAuthorizationFilter
{
    private readonly TeamRole _minimumRequiredRole;
    private readonly bool _requireTeamIdFromRoute;

    /// <summary>
    /// Initializes a new instance of the RequireTeamAccessAttribute.
    /// </summary>
    /// <param name="minimumRequiredRole">The minimum team role required to access the resource</param>
    /// <param name="requireTeamIdFromRoute">Whether to validate that the team ID in the route matches the user's team context</param>
    public RequireTeamAccessAttribute(TeamRole minimumRequiredRole = TeamRole.TeamMember, bool requireTeamIdFromRoute = true)
    {
        _minimumRequiredRole = minimumRequiredRole;
        _requireTeamIdFromRoute = requireTeamIdFromRoute;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user is a global admin (bypasses team restrictions)
        var isGlobalAdminClaim = context.HttpContext.User.FindFirst("is_global_admin");
        if (isGlobalAdminClaim != null && 
            bool.TryParse(isGlobalAdminClaim.Value, out var isGlobalAdmin) && 
            isGlobalAdmin)
        {
            // Global admin has access to all teams - allow access
            return;
        }

        // Get user's team ID from JWT claims
        var userTeamIdClaim = context.HttpContext.User.FindFirst("team_id");
        if (userTeamIdClaim == null || !Guid.TryParse(userTeamIdClaim.Value, out var userTeamId))
        {
            context.Result = new ForbidResult("User is not associated with any team");
            return;
        }

        // Get user's team role from JWT claims
        var userTeamRoleClaim = context.HttpContext.User.FindFirst("team_role");
        if (userTeamRoleClaim == null || !Enum.TryParse<TeamRole>(userTeamRoleClaim.Value, out var userTeamRole))
        {
            context.Result = new ForbidResult("User team role is not specified or invalid");
            return;
        }

        // Validate team ID from route if required
        if (_requireTeamIdFromRoute)
        {
            var routeTeamId = GetTeamIdFromRoute(context);
            if (routeTeamId.HasValue && routeTeamId.Value != userTeamId)
            {
                context.Result = new ForbidResult("Access denied: User does not have access to the specified team");
                return;
            }
        }

        // Check if user's role meets the minimum required role
        if (!HasSufficientRole(userTeamRole, _minimumRequiredRole))
        {
            context.Result = new ForbidResult($"Access denied: Minimum required role is {_minimumRequiredRole}, but user has {userTeamRole}");
            return;
        }

        // User has sufficient team access - allow access
    }

    /// <summary>
    /// Extracts team ID from route parameters. Looks for common parameter names like 'teamId', 'id' (when controller is team-related).
    /// </summary>
    private static Guid? GetTeamIdFromRoute(AuthorizationFilterContext context)
    {
        var routeData = context.RouteData.Values;

        // Try common route parameter names for team ID
        var teamIdKeys = new[] { "teamId", "id" };
        
        foreach (var key in teamIdKeys)
        {
            if (routeData.TryGetValue(key, out var value) && 
                value != null && 
                Guid.TryParse(value.ToString(), out var teamId))
            {
                return teamId;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the user's role meets or exceeds the minimum required role.
    /// Role hierarchy: TeamOwner > TeamAdmin > TeamMember
    /// </summary>
    private static bool HasSufficientRole(TeamRole userRole, TeamRole minimumRequiredRole)
    {
        // Define role hierarchy (lower values = higher privileges)
        var roleHierarchy = new Dictionary<TeamRole, int>
        {
            { TeamRole.TeamOwner, 1 },
            { TeamRole.TeamAdmin, 2 },
            { TeamRole.TeamMember, 3 }
        };

        return roleHierarchy[userRole] <= roleHierarchy[minimumRequiredRole];
    }
} 