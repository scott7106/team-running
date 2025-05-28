using System.ComponentModel.DataAnnotations;

namespace TeamStride.Application.Teams.Dtos;

public class InitiateOwnershipTransferDto
{
    [Required]
    [EmailAddress]
    public required string NewOwnerEmail { get; set; }
    
    public string? NewOwnerFirstName { get; set; }
    public string? NewOwnerLastName { get; set; }
    
    [StringLength(500)]
    public string? Message { get; set; }
    
    // For team owners transferring to existing team members
    public Guid? ExistingMemberId { get; set; }
} 