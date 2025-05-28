using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Dtos;

/// <summary>
/// DTO for deleted teams that can be recovered
/// </summary>
public class DeletedTeamDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public TeamStatus Status { get; set; }
    public TeamTier Tier { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? DeletedOn { get; set; }
    public string? DeletedByUserEmail { get; set; }
    
    // Owner information
    public Guid OwnerId { get; set; }
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
} 