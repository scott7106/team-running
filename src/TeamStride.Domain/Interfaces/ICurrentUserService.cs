using TeamStride.Domain.Entities;

namespace TeamStride.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    string? FirstName { get; }
    string? LastName { get; }
    bool IsAuthenticated { get; }
    
    // Simplified Authorization Model Properties
    bool IsGlobalAdmin { get; }
    
    // Current team context - delegated to ICurrentTeamService
    Guid? CurrentTeamId { get; }
    TeamRole? CurrentTeamRole { get; }
    MemberType? CurrentMemberType { get; }
    
    // Helper methods for role checking - delegated to ICurrentTeamService
    bool IsTeamOwner { get; }
    bool IsTeamAdmin { get; }
    bool IsTeamMember { get; }
    
    // Team-related queries that delegate to ICurrentTeamService
    bool CanAccessTeam(Guid teamId);
    bool HasMinimumTeamRole(TeamRole minimumRole);
} 