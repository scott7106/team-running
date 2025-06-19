using System.ComponentModel.DataAnnotations;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Athletes.Dtos;

public class CreateAthleteDto
{
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    public AthleteRole Role { get; set; } = AthleteRole.Athlete;
    
    [Required]
    public Gender Gender { get; set; }
    
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    
    [Required]
    public DateTime DateOfBirth { get; set; }
    
    [Required]
    public GradeLevel GradeLevel { get; set; }
    
    public CreateAthleteProfileDto? Profile { get; set; }
} 