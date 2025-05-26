using System;
using System.Collections.Generic;

namespace TeamStride.Domain.Identity;

public class Tenant
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Subdomain { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public required string SubscriptionTier { get; set; } // Free, Standard, Premium
    
    public virtual ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
} 