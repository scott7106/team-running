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
    
    // Team-related queries that delegate to ICurrentTeamService
    bool CanAccessTeam(Guid teamId);
    bool HasMinimumTeamRole(TeamRole minimumRole);
    Guid? CurrentTeamId { get; }
} 