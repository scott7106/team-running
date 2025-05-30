using System.ComponentModel.DataAnnotations;

namespace TeamStride.Application.Users.Dtos;

/// <summary>
/// DTO for setting all application roles for a user.
/// </summary>
public class SetUserRolesDto
{
    /// <summary>
    /// List of role names to assign to the user. Any existing roles not in this list will be removed.
    /// </summary>
    [Required]
    public List<string> RoleNames { get; set; } = new();
    
    /// <summary>
    /// Optional reason for the role change (for audit purposes).
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
} 