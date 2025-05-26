namespace TeamStride.Application.Tenants.Dtos;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#000000";
    public string SecondaryColor { get; set; } = "#FFFFFF";
    public string Status { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public DateTime? ExpiresOn { get; set; }
} 