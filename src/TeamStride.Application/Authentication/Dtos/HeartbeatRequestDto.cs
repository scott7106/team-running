using System.ComponentModel.DataAnnotations;

namespace TeamStride.Application.Authentication.Dtos;

public class HeartbeatRequestDto
{
    [Required]
    [MaxLength(10000)]  // Temporarily increased to test if length is the issue
    public required string Fingerprint { get; set; }
} 