using System.ComponentModel.DataAnnotations;

namespace TeamStride.Application.Users.Dtos;

/// <summary>
/// DTO for password reset operations via global admin interface.
/// </summary>
public class GlobalAdminResetPasswordDto
{
    /// <summary>
    /// New temporary password. If not provided, a random password will be generated.
    /// </summary>
    [StringLength(100, MinimumLength = 8)]
    public string? NewPassword { get; set; }

    /// <summary>
    /// If true, the user will be required to change the password on next login.
    /// </summary>
    [Required]
    public bool RequirePasswordChange { get; set; } = true;

    /// <summary>
    /// If true, send the new password to the user via email.
    /// </summary>
    public bool SendPasswordByEmail { get; set; } = false;

    /// <summary>
    /// If true, send the new password to the user via SMS (if phone number is available).
    /// </summary>
    public bool SendPasswordBySms { get; set; } = false;
} 