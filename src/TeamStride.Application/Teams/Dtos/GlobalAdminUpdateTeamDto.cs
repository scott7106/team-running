using System.ComponentModel.DataAnnotations;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Dtos;

/// <summary>
/// DTO for updating team properties (Global Admin only)
/// </summary>
public class GlobalAdminUpdateTeamDto
{
    [StringLength(100, MinimumLength = 2)]
    public string? Name { get; set; }
    
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Subdomain can only contain lowercase letters, numbers, and hyphens")]
    public string? Subdomain { get; set; }
    
    public TeamStatus? Status { get; set; }
    public TeamTier? Tier { get; set; }
    public DateTime? ExpiresOn { get; set; }
    
    [StringLength(7, MinimumLength = 7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string? PrimaryColor { get; set; }
    
    [StringLength(7, MinimumLength = 7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string? SecondaryColor { get; set; }
    
    [Url]
    public string? LogoUrl { get; set; }
} 