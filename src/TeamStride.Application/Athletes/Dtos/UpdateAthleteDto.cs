using System.ComponentModel.DataAnnotations;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Athletes.Dtos;

public class UpdateAthleteDto
{
    [StringLength(100, MinimumLength = 1)]
    public string? FirstName { get; set; }
    
    [StringLength(100, MinimumLength = 1)]
    public string? LastName { get; set; }
    
    public AthleteRole? Role { get; set; }
    public Gender? Gender { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public bool? HasPhysicalOnFile { get; set; }
    public bool? HasWaiverSigned { get; set; }
    public UpdateAthleteProfileDto? Profile { get; set; }
} 