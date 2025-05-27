using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TeamStride.Domain.Interfaces;
using TeamStride.Domain.Entities;

namespace TeamStride.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return value != null ? Guid.Parse(value) : null;
        }
    }

    public string? UserEmail => _httpContextAccessor.HttpContext?.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    // Simplified Authorization Model Properties
    public bool IsGlobalAdmin
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("is_global_admin");
            return claim != null && bool.TryParse(claim.Value, out var isGlobalAdmin) && isGlobalAdmin;
        }
    }

    public Guid? TeamId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("team_id");
            return claim != null && Guid.TryParse(claim.Value, out var teamId) ? teamId : null;
        }
    }

    public TeamRole? TeamRole
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("team_role");
            return claim != null && Enum.TryParse<TeamRole>(claim.Value, out var role) ? role : null;
        }
    }

    public MemberType? MemberType
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("member_type");
            return claim != null && Enum.TryParse<MemberType>(claim.Value, out var memberType) ? memberType : null;
        }
    }

    // Helper Methods for Role Checking
    public bool IsTeamOwner => TeamRole == Domain.Entities.TeamRole.TeamOwner;

    public bool IsTeamAdmin => TeamRole == Domain.Entities.TeamRole.TeamAdmin;

    public bool IsTeamMember => TeamRole == Domain.Entities.TeamRole.TeamMember;

    public bool CanAccessTeam(Guid teamId)
    {
        // Global admins can access any team
        if (IsGlobalAdmin)
            return true;

        // Standard users can only access their assigned team
        return TeamId.HasValue && TeamId.Value == teamId;
    }

    public bool HasMinimumTeamRole(TeamRole minimumRole)
    {
        // Global admins bypass all team role requirements
        if (IsGlobalAdmin)
            return true;

        // Check if user has a team role
        if (!TeamRole.HasValue)
            return false;

        // Define role hierarchy (lower values = higher privileges)
        var roleHierarchy = new Dictionary<TeamRole, int>
        {
            { Domain.Entities.TeamRole.TeamOwner, 1 },
            { Domain.Entities.TeamRole.TeamAdmin, 2 },
            { Domain.Entities.TeamRole.TeamMember, 3 }
        };

        return roleHierarchy[TeamRole.Value] <= roleHierarchy[minimumRole];
    }
} 