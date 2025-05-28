using System.ComponentModel.DataAnnotations;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Dtos;

public class UpdateSubscriptionDto
{
    [Required]
    public required TeamTier NewTier { get; set; }
    
    public DateTime? ExpiresOn { get; set; }
} 