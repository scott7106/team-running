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
    public string? JerseyNumber { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Grade { get; set; }
    public CreateAthleteProfileDto? Profile { get; set; }
} 