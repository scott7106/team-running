using Microsoft.AspNetCore.Identity;
using System;

namespace TeamStride.Domain.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() : base()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }

    // Additional properties
    public string? Description { get; set; }
} 