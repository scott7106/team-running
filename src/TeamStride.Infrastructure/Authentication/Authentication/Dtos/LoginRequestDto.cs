using System.ComponentModel.DataAnnotations;

namespace TeamStride.Application.Authentication.Dtos;

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? TenantId { get; set; }
} 