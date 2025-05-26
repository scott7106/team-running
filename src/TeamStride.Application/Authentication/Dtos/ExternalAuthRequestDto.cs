using System.ComponentModel.DataAnnotations;

namespace TeamStride.Application.Authentication.Dtos;

public class ExternalAuthRequestDto
{
    [Required]
    public required string Provider { get; set; }

    [Required]
    public required string AccessToken { get; set; }

    public Guid? TenantId { get; set; }
} 