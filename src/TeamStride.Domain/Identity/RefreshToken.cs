using System;

namespace TeamStride.Domain.Identity;

public class RefreshToken
{
    public required string Token { get; set; }
    public required Guid UserId { get; set; }
    public required Guid TenantId { get; set; }
    public DateTime CreatedOn { get; set; }
    public required string CreatedByIp { get; set; }
    public DateTime ExpiresOn { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? ReasonRevoked { get; set; }
    public DateTime? RevokedOn { get; set; }
    public bool IsActive => !RevokedOn.HasValue && DateTime.UtcNow < ExpiresOn;

    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
} 