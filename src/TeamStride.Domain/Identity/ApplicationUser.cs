using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace TeamStride.Domain.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DefaultTenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public virtual ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
} 