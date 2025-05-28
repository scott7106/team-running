using System.ComponentModel.DataAnnotations;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Dtos;

/// <summary>
/// DTO for creating a team with a new user as owner
/// </summary>
public class CreateTeamWithNewOwnerDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public required string Name { get; set; }
    
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Subdomain can only contain lowercase letters, numbers, and hyphens")]
    public required string Subdomain { get; set; }
    
    [Required]
    [EmailAddress]
    public required string OwnerEmail { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public required string OwnerFirstName { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public required string OwnerLastName { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string OwnerPassword { get; set; }
    
    public TeamTier Tier { get; set; } = TeamTier.Free;
    public string PrimaryColor { get; set; } = "#000000";
    public string SecondaryColor { get; set; } = "#FFFFFF";
    public DateTime? ExpiresOn { get; set; }
} 