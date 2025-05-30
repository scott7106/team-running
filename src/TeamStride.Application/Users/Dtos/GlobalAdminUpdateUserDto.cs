using System.ComponentModel.DataAnnotations;
using TeamStride.Domain.Identity;

namespace TeamStride.Application.Users.Dtos;

/// <summary>
/// DTO for updating user information via global admin interface.
/// </summary>
public class GlobalAdminUpdateUserDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; set; }

    [Required]
    [StringLength(50)]
    public required string FirstName { get; set; }

    [Required]
    [StringLength(50)]
    public required string LastName { get; set; }

    [Phone]
    [StringLength(15)]
    public string? PhoneNumber { get; set; }

    [Required]
    public UserStatus Status { get; set; }

    [Required]
    public bool IsActive { get; set; }

    [Required]
    public bool EmailConfirmed { get; set; }

    public bool PhoneNumberConfirmed { get; set; }
} 