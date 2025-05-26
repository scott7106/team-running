using System;

namespace TeamStride.Domain.Identity;

public class RefreshToken
{
    public required string Token { get; set; }
    public required string UserId { get; set; }
    public required string TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string CreatedByIp { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? ReasonRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsActive => !RevokedAt.HasValue && DateTime.UtcNow < ExpiresAt;

    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
} 