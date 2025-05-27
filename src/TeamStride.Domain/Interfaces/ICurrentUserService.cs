using TeamStride.Domain.Entities;

namespace TeamStride.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    bool IsAuthenticated { get; }
    
    // Simplified Authorization Model Properties
    bool IsGlobalAdmin { get; }
    Guid? TeamId { get; }
    TeamRole? TeamRole { get; }
    MemberType? MemberType { get; }
    
    // Helper Methods for Role Checking
    bool IsTeamOwner { get; }
    bool IsTeamAdmin { get; }
    bool IsTeamMember { get; }
    bool CanAccessTeam(Guid teamId);
    bool HasMinimumTeamRole(TeamRole minimumRole);
} 