using TeamStride.Application.Common.Models;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Services;

public interface ITeamManagementService
{
    // Team Listing & Retrieval
    Task<PaginatedList<TeamManagementDto>> GetTeamsAsync(
        int pageNumber = 1, 
        int pageSize = 10, 
        string? searchQuery = null,
        TeamStatus? status = null,
        TeamTier? tier = null);
    
    Task<TeamManagementDto> GetTeamByIdAsync(Guid teamId);
    Task<TeamManagementDto> GetTeamBySubdomainAsync(string subdomain);

    // Team Creation & Basic Management
    Task<TeamManagementDto> CreateTeamAsync(CreateTeamDto dto);
    Task<TeamManagementDto> UpdateTeamAsync(Guid teamId, UpdateTeamDto dto);
    Task DeleteTeamAsync(Guid teamId);

    // Team Ownership Management
    Task<OwnershipTransferDto> InitiateOwnershipTransferAsync(Guid teamId, TransferOwnershipDto dto);
    Task<TeamManagementDto> CompleteOwnershipTransferAsync(string transferToken);
    Task CancelOwnershipTransferAsync(Guid transferId);
    Task<IEnumerable<OwnershipTransferDto>> GetPendingTransfersAsync(Guid teamId);

    // Team Subscription Management
    Task<TeamManagementDto> UpdateSubscriptionAsync(Guid teamId, UpdateSubscriptionDto dto);

    // Team Branding Management
    Task<TeamManagementDto> UpdateBrandingAsync(Guid teamId, UpdateTeamBrandingDto dto);

    // Team Member Management
    Task<PaginatedList<TeamMemberDto>> GetTeamMembersAsync(
        Guid teamId, 
        int pageNumber = 1, 
        int pageSize = 10,
        TeamRole? role = null);
    
    Task<TeamMemberDto> UpdateMemberRoleAsync(Guid teamId, Guid userId, TeamRole newRole);
    Task RemoveMemberAsync(Guid teamId, Guid userId);

    // Subdomain Management
    Task<bool> IsSubdomainAvailableAsync(string subdomain);
    Task<TeamManagementDto> UpdateSubdomainAsync(Guid teamId, string newSubdomain);
    
    // Tier Limits & Validation
    Task<TeamTierLimitsDto> GetTierLimitsAsync(TeamTier tier);
    Task<bool> CanAddAthleteAsync(Guid teamId);
} 