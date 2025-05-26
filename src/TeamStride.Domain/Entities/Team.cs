using TeamStride.Domain.Common;

namespace TeamStride.Domain.Entities;

public class Team : AuditedEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#000000";
    public string SecondaryColor { get; set; } = "#FFFFFF";
    public TeamStatus Status { get; set; }
    public TeamTier Tier { get; set; }
    public DateTime? ExpiresOn { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserTeam> Users { get; set; } = new List<UserTeam>();
}

public enum TeamStatus
{
    Active,
    Suspended,
    Expired,
    PendingSetup
}

public enum TeamTier
{
    Free,
    Standard,
    Premium
} 