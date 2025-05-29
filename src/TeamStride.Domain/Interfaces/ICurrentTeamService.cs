using TeamStride.Domain.Entities;

namespace TeamStride.Domain.Interfaces;

public interface ICurrentTeamService
{
    // Team Context Properties
    Guid TeamId { get; }
    string? GetSubdomain { get; }
    bool IsTeamSet { get; }
    
    // User's Role in Current Team (from JWT claims)
    TeamRole? TeamRole { get; }
    MemberType? MemberType { get; }
    
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