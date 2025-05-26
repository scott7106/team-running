using TeamStride.Domain.Common;

namespace TeamStride.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#000000";
    public string SecondaryColor { get; set; } = "#FFFFFF";
    public TenantStatus Status { get; set; }
    public TenantTier Tier { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<TenantUser> Users { get; set; } = new List<TenantUser>();
}

public enum TenantStatus
{
    Active,
    Suspended,
    Expired,
    PendingSetup
}

public enum TenantTier
{
    Free,
    Standard,
    Premium
} 