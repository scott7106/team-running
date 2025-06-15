namespace TeamStride.Application.Teams.Dtos;

/// <summary>
/// DTO for public team creation result
/// </summary>
public class PublicTeamCreationResultDto
{
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string TeamSubdomain { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
} 