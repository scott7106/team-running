using System.ComponentModel.DataAnnotations;

namespace TeamStride.Application.Authentication.Dtos;

public class ExternalAuthRequestDto
{
    [Required]
    public string Provider { get; set; } = string.Empty;

    [Required]
    public string AccessToken { get; set; } = string.Empty;

    public string? TenantId { get; set; }
}

public enum ExternalAuthProvider
{
    Microsoft,
    Google,
    Facebook,
    Twitter
} 