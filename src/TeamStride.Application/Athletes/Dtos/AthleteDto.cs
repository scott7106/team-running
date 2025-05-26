using System.ComponentModel.DataAnnotations;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Athletes.Dtos;

public class AthleteDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public AthleteRole Role { get; set; }
    public string? JerseyNumber { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Grade { get; set; }
    public bool HasPhysicalOnFile { get; set; }
    public bool HasWaiverSigned { get; set; }
    public AthleteProfileDto? Profile { get; set; }
}

public class CreateAthleteDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string FirstName { get; set; }

    [Required]
    public required string LastName { get; set; }

    public AthleteRole Role { get; set; } = AthleteRole.Athlete;
    public string? JerseyNumber { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Grade { get; set; }
    public CreateAthleteProfileDto? Profile { get; set; }
}

public class UpdateAthleteDto
{
    public AthleteRole? Role { get; set; }
    public string? JerseyNumber { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Grade { get; set; }
    public bool? HasPhysicalOnFile { get; set; }
    public bool? HasWaiverSigned { get; set; }
    public UpdateAthleteProfileDto? Profile { get; set; }
} 