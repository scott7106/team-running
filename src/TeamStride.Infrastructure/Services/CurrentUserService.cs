using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TeamStride.Domain.Interfaces;
using TeamStride.Domain.Entities;

namespace TeamStride.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentTeamService _currentTeamService;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        ICurrentTeamService currentTeamService)
    {
        _httpContextAccessor = httpContextAccessor;
        _currentTeamService = currentTeamService;
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

    public string? FirstName => _httpContextAccessor.HttpContext?.User?.FindFirst("first_name")?.Value;

    public string? LastName => _httpContextAccessor.HttpContext?.User?.FindFirst("last_name")?.Value;

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

    // Current team context - delegate to CurrentTeamService
    public Guid? CurrentTeamId => _currentTeamService.IsTeamSet ? _currentTeamService.TeamId : null;

    // Current team role and member type - delegate to CurrentTeamService
    public TeamRole? CurrentTeamRole => _currentTeamService.CurrentTeamRole;
    public MemberType? CurrentMemberType => _currentTeamService.CurrentMemberType;

    // Helper Methods for Role Checking - delegate to CurrentTeamService
    public bool IsTeamOwner => _currentTeamService.IsTeamOwner;
    public bool IsTeamAdmin => _currentTeamService.IsTeamAdmin;
    public bool IsTeamMember => _currentTeamService.IsTeamMember;

    // Delegate team-related queries to ICurrentTeamService
    public bool CanAccessTeam(Guid teamId) => _currentTeamService.CanAccessTeam(teamId);

    public bool HasMinimumTeamRole(TeamRole minimumRole) => _currentTeamService.HasMinimumTeamRole(minimumRole);
} 