using System.ComponentModel.DataAnnotations;

namespace TeamStride.Application.Teams.Dtos;

/// <summary>
/// DTO for initiating ownership transfer (Global Admin only)
/// </summary>
public class GlobalAdminTransferOwnershipDto
{
    [Required]
    public required Guid NewOwnerId { get; set; }
    
    [StringLength(500)]
    public string? Message { get; set; }
} 