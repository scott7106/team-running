using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TeamStride.Domain.Interfaces;
using TeamStride.Domain.Entities;
using TeamStride.Application.Teams.Services;
using System.Text.Json;

namespace TeamStride.Infrastructure.Services;

public class CurrentTeamService : ICurrentTeamService
{
    private readonly ILogger<CurrentTeamService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private Guid? _currentTeamId;
    private string? _currentSubdomain;

    public CurrentTeamService(
        ILogger<CurrentTeamService> logger,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public Guid TeamId
    {
        get
        {
            if (!_currentTeamId.HasValue)
            {
                throw new InvalidOperationException("Current team is not set");
            }
            return _currentTeamId.Value;
        }
    }

    public string? GetSubdomain => _currentSubdomain;

    public bool IsTeamSet => _currentTeamId.HasValue;

    // Parse team memberships from JWT claims
    public List<TeamMembershipInfo> GetTeamMemberships()
    {
        var memberships = new List<TeamMembershipInfo>();
        
        if (!IsAuthenticated)
        {
            _logger.LogDebug("User not authenticated, returning empty team memberships");
            return memberships;
        }

        try
        {
            var teamMembershipsJson = _httpContextAccessor.HttpContext?.User?.FindFirst("team_memberships")?.Value;
            _logger.LogWarning("Raw team memberships claim value: {TeamMembershipsJson}", teamMembershipsJson ?? "null");
            
            if (string.IsNullOrEmpty(teamMembershipsJson))
            {
                _logger.LogWarning("Team memberships claim is null or empty");
                return memberships;
            }

            var teamMembershipDtos = JsonSerializer.Deserialize<List<dynamic>>(teamMembershipsJson);
            if (teamMembershipDtos == null) 
            {
                _logger.LogWarning("Failed to deserialize team memberships JSON");
                return memberships;
            }

            _logger.LogWarning("Parsed {Count} team membership entries from JSON", teamMembershipDtos.Count);

            foreach (var dto in teamMembershipDtos)
            {
                try
                {
                    var jsonElement = (JsonElement)dto;
                    _logger.LogWarning("Processing membership JSON element: {JsonElement}", jsonElement.ToString());
                    
                    // Parse teamId - handle both string and GUID formats
                    Guid teamId;
                    if (jsonElement.TryGetProperty("teamId", out var teamIdProp))
                    {
                        if (teamIdProp.ValueKind == JsonValueKind.String)
                        {
                            if (!Guid.TryParse(teamIdProp.GetString(), out teamId))
                            {
                                _logger.LogWarning("Failed to parse teamId as GUID from string: {TeamIdValue}", teamIdProp.GetString());
                                continue;
                            }
                        }
                        else if (teamIdProp.ValueKind == JsonValueKind.Number)
                        {
                            _logger.LogWarning("TeamId is stored as number, but expected string/GUID: {TeamIdValue}", teamIdProp.ToString());
                            continue;
                        }
                        else
                        {
                            _logger.LogWarning("TeamId has unexpected value kind: {ValueKind}, value: {Value}", teamIdProp.ValueKind, teamIdProp.ToString());
                            continue;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Missing teamId property in membership JSON");
                        continue;
                    }

                    // Parse other required properties
                    if (!jsonElement.TryGetProperty("teamSubdomain", out var subdomainProp) ||
                        !jsonElement.TryGetProperty("teamRole", out var roleProp) ||
                        !jsonElement.TryGetProperty("memberType", out var memberTypeProp))
                    {
                        _logger.LogWarning("Missing required properties in membership JSON: {JsonElement}", jsonElement.ToString());
                        continue;
                    }

                    // Parse teamRole - handle both string and number formats
                    TeamRole teamRole;
                    if (roleProp.ValueKind == JsonValueKind.String)
                    {
                        if (!Enum.TryParse<TeamRole>(roleProp.GetString(), out teamRole))
                        {
                            _logger.LogWarning("Failed to parse teamRole from string: {TeamRoleValue}", roleProp.GetString());
                            continue;
                        }
                    }
                    else if (roleProp.ValueKind == JsonValueKind.Number)
                    {
                        var roleNumber = roleProp.GetInt32();
                        if (!Enum.IsDefined(typeof(TeamRole), roleNumber))
                        {
                            _logger.LogWarning("Invalid teamRole number: {TeamRoleValue}", roleNumber);
                            continue;
                        }
                        teamRole = (TeamRole)roleNumber;
                        _logger.LogWarning("Parsed teamRole from number: {RoleNumber} = {RoleName}", roleNumber, teamRole);
                    }
                    else
                    {
                        _logger.LogWarning("TeamRole has unexpected value kind: {ValueKind}, value: {Value}", roleProp.ValueKind, roleProp.ToString());
                        continue;
                    }

                    // Parse memberType - handle both string and number formats
                    MemberType memberType;
                    if (memberTypeProp.ValueKind == JsonValueKind.String)
                    {
                        if (!Enum.TryParse<MemberType>(memberTypeProp.GetString(), out memberType))
                        {
                            _logger.LogWarning("Failed to parse memberType from string: {MemberTypeValue}", memberTypeProp.GetString());
                            continue;
                        }
                    }
                    else if (memberTypeProp.ValueKind == JsonValueKind.Number)
                    {
                        var memberTypeNumber = memberTypeProp.GetInt32();
                        if (!Enum.IsDefined(typeof(MemberType), memberTypeNumber))
                        {
                            _logger.LogWarning("Invalid memberType number: {MemberTypeValue}", memberTypeNumber);
                            continue;
                        }
                        memberType = (MemberType)memberTypeNumber;
                        _logger.LogWarning("Parsed memberType from number: {MemberTypeNumber} = {MemberTypeName}", memberTypeNumber, memberType);
                    }
                    else
                    {
                        _logger.LogWarning("MemberType has unexpected value kind: {ValueKind}, value: {Value}", memberTypeProp.ValueKind, memberTypeProp.ToString());
                        continue;
                    }

                    var membership = new TeamMembershipInfo(
                        teamId,
                        subdomainProp.GetString() ?? string.Empty,
                        teamRole,
                        memberType);
                    
                    memberships.Add(membership);
                    _logger.LogWarning("Successfully parsed membership: TeamId={TeamId}, Subdomain={Subdomain}, Role={Role}, MemberType={MemberType}", 
                        teamId, subdomainProp.GetString(), teamRole, memberType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing individual team membership entry");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse team memberships from JWT claims. Raw JSON: {RawJson}", 
                _httpContextAccessor.HttpContext?.User?.FindFirst("team_memberships")?.Value ?? "null");
        }

        _logger.LogDebug("Returning {Count} team memberships", memberships.Count);
        return memberships;
    }

    // Get current team membership based on current subdomain
    private TeamMembershipInfo? GetCurrentTeamMembership()
    {
        if (string.IsNullOrEmpty(_currentSubdomain))
        {
            return null;
        }

        var memberships = GetTeamMemberships();
        return memberships.FirstOrDefault(m => m.TeamSubdomain.Equals(_currentSubdomain, StringComparison.OrdinalIgnoreCase));
    }

    // Team Role Properties from Current Team Membership
    public TeamRole? CurrentTeamRole
    {
        get
        {
            return GetCurrentTeamMembership()?.TeamRole;
        }
    }

    public MemberType? CurrentMemberType
    {
        get
        {
            return GetCurrentTeamMembership()?.MemberType;
        }
    }

    // Helper Properties for Current Team
    public bool IsTeamOwner => CurrentTeamRole == Domain.Entities.TeamRole.TeamOwner;
    public bool IsTeamAdmin => CurrentTeamRole == Domain.Entities.TeamRole.TeamAdmin;
    public bool IsTeamMember => CurrentTeamRole == Domain.Entities.TeamRole.TeamMember;

    // Team Context Management Methods
    public void SetTeamId(Guid teamId)
    {
        _currentTeamId = teamId;
        _logger.LogInformation("Current team set to {TeamId}", teamId);
    }

    public void SetTeamSubdomain(string subdomain)
    {
        _currentSubdomain = subdomain;
        _logger.LogInformation("Current team subdomain set to {Subdomain}", subdomain);
    }

    public async Task<bool> SetTeamFromSubdomainAsync(string subdomain)
    {
        try
        {
            var standardTeamService = _serviceProvider.GetService(typeof(IStandardTeamService)) as IStandardTeamService;
            if (standardTeamService == null)
            {
                _logger.LogWarning("StandardTeamService not available for team resolution");
                return false;
            }

            var team = await standardTeamService.GetTeamBySubdomainAsync(subdomain);
            if (team != null)
            {
                _currentTeamId = team.Id;
                _currentSubdomain = subdomain;
                _logger.LogInformation("Team context set from subdomain {Subdomain} to team {TeamId}", subdomain, team.Id);
                return true;
            }

            _logger.LogWarning("No team found for subdomain: {Subdomain}", subdomain);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving team from subdomain: {Subdomain}", subdomain);
            return false;
        }
    }

    public bool SetTeamFromJwtClaims()
    {
        try
        {
            _logger.LogDebug("Starting SetTeamFromJwtClaims. Current subdomain: {Subdomain}, IsAuthenticated: {IsAuthenticated}", 
                _currentSubdomain, IsAuthenticated);

            if (!IsAuthenticated)
            {
                _logger.LogDebug("User not authenticated, cannot set team from JWT claims");
                return false;
            }

            // Global admins must have team membership to access team context, same as regular users

            // Get all team memberships first
            var memberships = GetTeamMemberships();
            if (!memberships.Any())
            {
                _logger.LogWarning("User has no team memberships in JWT claims");
                return false;
            }

            // For regular users, the current team is determined by subdomain matching
            if (!string.IsNullOrEmpty(_currentSubdomain))
            {
                _logger.LogDebug("Looking for team membership matching subdomain: {Subdomain}", _currentSubdomain);
                var currentMembership = GetCurrentTeamMembership();
                if (currentMembership != null)
                {
                    _logger.LogInformation("Found matching team membership. Setting team {TeamId} for subdomain {Subdomain}", 
                        currentMembership.TeamId, _currentSubdomain);
                    _currentTeamId = currentMembership.TeamId;
                    _logger.LogInformation("Team context validated from JWT claims for subdomain {Subdomain} to team {TeamId}", _currentSubdomain, currentMembership.TeamId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("No team membership found matching subdomain {Subdomain}. Available memberships: {MembershipCount}", 
                        _currentSubdomain, memberships.Count);
                    foreach (var membership in memberships)
                    {
                        _logger.LogDebug("Available membership: TeamId={TeamId}, Subdomain={TeamSubdomain}, Role={Role}", 
                            membership.TeamId, membership.TeamSubdomain, membership.TeamRole);
                    }
                }
            }
            else
            {
                _logger.LogWarning("Current subdomain is null or empty. Attempting fallback strategy with {MembershipCount} available memberships", memberships.Count);
                
                // Fallback strategy: If user has only one team membership, use that team
                if (memberships.Count == 1)
                {
                    var singleMembership = memberships.First();
                    _logger.LogInformation("Using single team membership as fallback. Setting team {TeamId} (subdomain: {Subdomain})", 
                        singleMembership.TeamId, singleMembership.TeamSubdomain);
                    _currentTeamId = singleMembership.TeamId;
                    _currentSubdomain = singleMembership.TeamSubdomain; // Set the subdomain for consistency
                    return true;
                }
                else
                {
                    _logger.LogWarning("User has {MembershipCount} team memberships but no subdomain context. Cannot determine which team to use", memberships.Count);
                    foreach (var membership in memberships)
                    {
                        _logger.LogDebug("Available membership: TeamId={TeamId}, Subdomain={TeamSubdomain}, Role={Role}", 
                            membership.TeamId, membership.TeamSubdomain, membership.TeamRole);
                    }
                }
            }

            _logger.LogWarning("No matching team membership found for current subdomain {Subdomain}", _currentSubdomain);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting team from JWT claims");
            return false;
        }
    }

    public void ClearTeam()
    {
        _currentTeamId = null;
        _currentSubdomain = null;
        _logger.LogInformation("Current team cleared");
    }

    // Authorization Methods
    public bool CanAccessCurrentTeam()
    {
        if (!IsTeamSet) return false;
        return CanAccessTeam(TeamId);
    }

    public bool HasMinimumTeamRole(TeamRole minimumRole)
    {
        if (!CurrentTeamRole.HasValue) return false;

        var roleHierarchy = new Dictionary<TeamRole, int>
        {
            { Domain.Entities.TeamRole.TeamOwner, 1 },
            { Domain.Entities.TeamRole.TeamAdmin, 2 },
            { Domain.Entities.TeamRole.TeamMember, 3 }
        };

        return roleHierarchy[CurrentTeamRole.Value] <= roleHierarchy[minimumRole];
    }

    public bool CanAccessTeam(Guid teamId)
    {
        // Check if user has membership in the specified team
        var memberships = GetTeamMemberships();
        return memberships.Any(m => m.TeamId == teamId);
    }

    // Private Helper Methods
    private bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    private bool IsGlobalAdmin
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("is_global_admin");
            return claim != null && bool.TryParse(claim.Value, out var isGlobalAdmin) && isGlobalAdmin;
        }
    }
} 