using System.ComponentModel.DataAnnotations;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Dtos;

public class UpdateTeamDto
{
    [StringLength(100, MinimumLength = 2)]
    public string? Name { get; set; }
    
    public TeamStatus? Status { get; set; }
    public DateTime? ExpiresOn { get; set; }
} 