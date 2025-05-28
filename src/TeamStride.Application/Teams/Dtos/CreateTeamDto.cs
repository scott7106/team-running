using System.ComponentModel.DataAnnotations;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Dtos;

public class CreateTeamDto
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
    
    public string? OwnerFirstName { get; set; }
    public string? OwnerLastName { get; set; }
    
    public TeamTier Tier { get; set; } = TeamTier.Free;
    public string PrimaryColor { get; set; } = "#000000";
    public string SecondaryColor { get; set; } = "#FFFFFF";
} 