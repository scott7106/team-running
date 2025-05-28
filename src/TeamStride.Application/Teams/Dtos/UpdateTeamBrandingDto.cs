using System.ComponentModel.DataAnnotations;

namespace TeamStride.Application.Teams.Dtos;

public class UpdateTeamBrandingDto
{
    [StringLength(7, MinimumLength = 7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string? PrimaryColor { get; set; }
    
    [StringLength(7, MinimumLength = 7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string? SecondaryColor { get; set; }
    
    [Url]
    public string? LogoUrl { get; set; }
} 