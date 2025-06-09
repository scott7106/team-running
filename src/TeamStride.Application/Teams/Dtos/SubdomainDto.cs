namespace TeamStride.Application.Teams.Dtos;

public class SubdomainDto
{
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = "#3B82F6";
    public string SecondaryColor { get; set; } = "#D1FAE5";
    public string LogoUrl { get; set; } = string.Empty;
} 