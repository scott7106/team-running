using System.ComponentModel.DataAnnotations;
using TeamStride.Domain.Identity;

namespace TeamStride.Application.Users.Dtos;

/// <summary>
/// DTO for creating new users via global admin interface.
/// </summary>
public class GlobalAdminCreateUserDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; set; }

    [Required]
    [StringLength(50)]
    public required string FirstName { get; set; }

    [Required]
    [StringLength(50)]
    public required string LastName { get; set; }

    [Phone]
    [StringLength(15)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Application roles to assign to the user upon creation.
    /// </summary>
    public List<string> ApplicationRoles { get; set; } = new();

    /// <summary>
    /// If true, the user will be required to change the password on first login.
    /// </summary>
    public bool RequirePasswordChange { get; set; } = true;

} 