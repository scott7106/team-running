using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Dtos;

public class TeamDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#000000";
    public string SecondaryColor { get; set; } = "#FFFFFF";
    public TeamStatus Status { get; set; }
    public TeamTier Tier { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
    
    // Owner information
    public TeamMemberDto? Owner { get; set; }
    
    // Statistics
    public int MemberCount { get; set; }
    public int AthleteCount { get; set; }
    public int AdminCount { get; set; }
    
    // Pending transfers
    public bool HasPendingOwnershipTransfer { get; set; }
} 