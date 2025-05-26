using System.ComponentModel.DataAnnotations;

namespace TeamStride.Application.Authentication.Dtos;

public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; set; }

    [Required]
    [Compare("Password")]
    public required string ConfirmPassword { get; set; }

    [Required]
    public required string FirstName { get; set; }

    [Required]
    public required string LastName { get; set; }

    [Required]
    public required string TenantId { get; set; }

    [Required]
    public required string Role { get; set; }
} 