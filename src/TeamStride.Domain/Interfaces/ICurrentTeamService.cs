using TeamStride.Domain.Entities;

namespace TeamStride.Domain.Interfaces;

public record TeamMembershipInfo(Guid TeamId, string TeamSubdomain, TeamRole TeamRole, MemberType MemberType);

public interface ICurrentTeamService
{
    // Team Context Properties
    Guid TeamId { get; }
    string? GetSubdomain { get; }
    bool IsTeamSet { get; }
    
    // User's Role in Current Team (from team memberships in JWT claims)
    TeamRole? CurrentTeamRole { get; }
    MemberType? CurrentMemberType { get; }
    
    // Parse team memberships from JWT claims
    List<TeamMembershipInfo> GetTeamMemberships();
    
    // Team Context Management
    void SetTeamId(Guid teamId);
    void SetTeamSubdomain(string subdomain);
    Task<bool> SetTeamFromSubdomainAsync(string subdomain);
    bool SetTeamFromJwtClaims();
    void ClearTeam();
    
    // Team Authorization Methods (considering current team context)
    bool IsTeamOwner { get; }
    bool IsTeamAdmin { get; }
    bool IsTeamMember { get; }
    bool CanAccessCurrentTeam();
    bool HasMinimumTeamRole(TeamRole minimumRole);
    
    // Cross-team Authorization Methods
    bool CanAccessTeam(Guid teamId);
} 