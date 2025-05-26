using System.ComponentModel.DataAnnotations;

namespace TeamStride.Application.Authentication.Dtos;

public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; }

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
} 